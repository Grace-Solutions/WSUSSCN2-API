using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using System.IO.Pipelines;
using System.Text;
using System.Xml;
using DotNetCab;

namespace rebuild;

public class RebuildService : IRebuildService
{
    private readonly AppDbContext _dbContext;
    private readonly IMinioClient _minioClient;
    private readonly ILogger<RebuildService> _logger;
    private const string CabBucketName = "rebuilt-cabs";

    public RebuildService(
        AppDbContext dbContext,
        IMinioClient minioClient,
        ILogger<RebuildService> logger)
    {
        _dbContext = dbContext;
        _minioClient = minioClient;
        _logger = logger;
    }

    public async Task RebuildCabsAsync(string groupStrategy, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting CAB rebuild process with strategy: {Strategy}", groupStrategy);

        try
        {
            // Ensure bucket exists
            await EnsureBucketExistsAsync(cancellationToken);

            // Get groups based on strategy
            var groups = await GetGroupsAsync(groupStrategy, cancellationToken);
            _logger.LogInformation("Found {Count} groups to process", groups.Count);

            foreach (var group in groups)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await RebuildCabForGroupAsync(groupStrategy, group, cancellationToken);
            }

            _logger.LogInformation("CAB rebuild process completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during CAB rebuild process");
            throw;
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var bucketExistsArgs = new BucketExistsArgs().WithBucket(CabBucketName);
        var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);
        if (!exists)
        {
            _logger.LogInformation("Creating bucket {BucketName}", CabBucketName);
            var makeBucketArgs = new MakeBucketArgs().WithBucket(CabBucketName);
            await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);
        }
    }

    private async Task<List<string>> GetGroupsAsync(string groupStrategy, CancellationToken cancellationToken)
    {
        // Get groups based on strategy
        return groupStrategy switch
        {
            "OS" => await _dbContext.Updates
                .Where(u => u.OsVersion != null)
                .Select(u => u.OsVersion!)
                .Distinct()
                .ToListAsync(cancellationToken),

            "Year" => await _dbContext.Updates
                .Where(u => u.Year != null)
                .Select(u => u.Year.ToString()!)
                .Distinct()
                .ToListAsync(cancellationToken),

            "Year-Month" => await _dbContext.Updates
                .Where(u => u.Year != null && u.Month != null)
                .Select(u => $"{u.Year}-{u.Month:D2}")
                .Distinct()
                .ToListAsync(cancellationToken),

            "ProductFamily" => await _dbContext.Updates
                .Where(u => u.ProductFamily != null)
                .Select(u => u.ProductFamily!)
                .Distinct()
                .ToListAsync(cancellationToken),

            "Year-OS" => await _dbContext.Updates
                .Where(u => u.Year != null && u.OsVersion != null)
                .Select(u => $"{u.Year}-{u.OsVersion}")
                .Distinct()
                .ToListAsync(cancellationToken),

            _ => await _dbContext.Updates
                .Where(u => u.Year != null && u.OsVersion != null)
                .Select(u => $"{u.Year}-{u.OsVersion}")
                .Distinct()
                .ToListAsync(cancellationToken)
        };
    }

    private async Task RebuildCabForGroupAsync(string groupStrategy, string groupValue, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rebuilding CAB for group: {Group}", groupValue);

        // Check if CAB already exists for this group
        var existingCab = await _dbContext.CabFiles
            .FirstOrDefaultAsync(c => c.GroupStrategy == groupStrategy && c.GroupValue == groupValue, cancellationToken);

        if (existingCab != null)
        {
            _logger.LogInformation("CAB already exists for group {Group}, skipping", groupValue);
            return;
        }

        // Get updates for this group
        var updates = await GetUpdatesForGroupAsync(groupStrategy, groupValue, cancellationToken);
        if (!updates.Any())
        {
            _logger.LogInformation("No updates found for group {Group}, skipping", groupValue);
            return;
        }

        // Create CAB file
        var (cabPath, cabSize) = await CreateCabFileAsync(updates, groupValue, cancellationToken);

        // Upload to MinIO
        var objectName = $"{groupStrategy.ToLower()}_{groupValue.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}.cab";
        await UploadToMinioAsync(cabPath, objectName, cancellationToken);

        // Create CAB file record
        var cabFile = new CabFile
        {
            FileName = objectName,
            GroupStrategy = groupStrategy,
            GroupValue = groupValue,
            MinioPath = objectName,
            SizeBytes = cabSize,
            UpdateCount = updates.Count,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.CabFiles.Add(cabFile);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Clean up
        try
        {
            File.Delete(cabPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cleaning up temp file {FilePath}", cabPath);
        }

        _logger.LogInformation("CAB rebuilt for group {Group} with {Count} updates", groupValue, updates.Count);
    }

    private async Task<List<Update>> GetUpdatesForGroupAsync(string groupStrategy, string groupValue, CancellationToken cancellationToken)
    {
        // Get updates based on group strategy and value
        return groupStrategy switch
        {
            "OS" => await _dbContext.Updates
                .Where(u => u.OsVersion == groupValue)
                .ToListAsync(cancellationToken),

            "Year" => await _dbContext.Updates
                .Where(u => u.Year.ToString() == groupValue)
                .ToListAsync(cancellationToken),

            "Year-Month" => await _dbContext.Updates
                .Where(u => $"{u.Year}-{u.Month:D2}" == groupValue)
                .ToListAsync(cancellationToken),

            "ProductFamily" => await _dbContext.Updates
                .Where(u => u.ProductFamily == groupValue)
                .ToListAsync(cancellationToken),

            "Year-OS" => await _dbContext.Updates
                .Where(u => $"{u.Year}-{u.OsVersion}" == groupValue)
                .ToListAsync(cancellationToken),

            _ => await _dbContext.Updates
                .Where(u => $"{u.Year}-{u.OsVersion}" == groupValue)
                .ToListAsync(cancellationToken)
        };
    }

    private async Task<(string FilePath, long FileSize)> CreateCabFileAsync(List<Update> updates, string groupValue, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating CAB file for group {Group} with {Count} updates using streaming", groupValue, updates.Count);

        // Create temp directory
        var tempDir = Path.Combine(Path.GetTempPath(), "wsusscn2-api", "rebuild");
        Directory.CreateDirectory(tempDir);

        var cabPath = Path.Combine(tempDir, $"{groupValue.Replace(" ", "_")}.cab");

        // Create a directory for files to include in the CAB
        var filesDir = Path.Combine(tempDir, "files");
        Directory.CreateDirectory(filesDir);

        // Create index.xml with streaming
        var indexXmlPath = Path.Combine(filesDir, "index.xml");
        using (var fileStream = new FileStream(indexXmlPath, FileMode.Create, FileAccess.Write))
        {
            await CreateIndexXmlStreamAsync(updates, fileStream, cancellationToken);
        }

        // Create other necessary files for WUA compatibility
        await CreateWuaCompatibilityFilesAsync(filesDir, updates, cancellationToken);

        // Create CAB file using DotNetCab
        await CreateCabWithDotNetCabAsync(cabPath, filesDir, cancellationToken);

        // Get file size
        var fileInfo = new FileInfo(cabPath);
        var fileSize = fileInfo.Length;

        _logger.LogInformation("Created CAB file at {FilePath} with size {Size} bytes", cabPath, fileSize);

        return (cabPath, fileSize);
    }

    private async Task CreateCabWithDotNetCabAsync(string cabPath, string filesDir, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating CAB file using DotNetCab");

        try
        {
            // Create a new CAB builder
            using var cabBuilder = new DotNetCab.CabBuilder(cabPath);

            // Add all files from the directory
            foreach (var filePath in Directory.GetFiles(filesDir))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var fileInfo = new FileInfo(filePath);
                var entryName = Path.GetFileName(filePath);

                _logger.LogDebug("Adding file {FileName} to CAB", entryName);

                // Add file to CAB using streams for memory efficiency
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    cabBuilder.AddStream(fileStream, entryName, fileInfo.LastWriteTime);
                }
            }

            // Build the CAB file
            cabBuilder.Build();

            _logger.LogInformation("Successfully created CAB file {CabPath}", cabPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating CAB file {CabPath}", cabPath);
            throw;
        }
    }

    private async Task CreateIndexXmlStreamAsync(List<Update> updates, Stream outputStream, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating index.xml with {Count} updates", updates.Count);

        using var writer = XmlWriter.Create(outputStream, new XmlWriterSettings
        {
            Async = true,
            Indent = true,
            Encoding = Encoding.UTF8
        });

        await writer.WriteStartDocumentAsync();
        await writer.WriteStartElementAsync(null, "Updates", null);

        foreach (var update in updates)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await writer.WriteStartElementAsync(null, "Update", null);
            await writer.WriteAttributeStringAsync(null, "UpdateId", null, update.UpdateId);

            await writer.WriteElementStringAsync(null, "Title", null, update.Title);

            if (!string.IsNullOrEmpty(update.Description))
                await writer.WriteElementStringAsync(null, "Description", null, update.Description);

            if (!string.IsNullOrEmpty(update.Classification))
                await writer.WriteElementStringAsync(null, "Classification", null, update.Classification);

            if (!string.IsNullOrEmpty(update.Product))
                await writer.WriteElementStringAsync(null, "Product", null, update.Product);

            if (!string.IsNullOrEmpty(update.ProductFamily))
                await writer.WriteElementStringAsync(null, "ProductFamily", null, update.ProductFamily);

            if (!string.IsNullOrEmpty(update.KbArticleId))
                await writer.WriteElementStringAsync(null, "KBArticleID", null, update.KbArticleId);

            if (update.ReleaseDate.HasValue)
                await writer.WriteElementStringAsync(null, "ReleaseDate", null, update.ReleaseDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"));

            // Write categories
            if (update.Categories != null && update.Categories.Length > 0)
            {
                await writer.WriteStartElementAsync(null, "Categories", null);

                foreach (var category in update.Categories)
                {
                    await writer.WriteElementStringAsync(null, "Category", null, category);
                }

                await writer.WriteEndElementAsync(); // Categories
            }

            // Write supersedence information
            if (update.SupersededBy != null && update.SupersededBy.Length > 0)
            {
                await writer.WriteStartElementAsync(null, "SupersededBy", null);

                foreach (var supersededBy in update.SupersededBy)
                {
                    await writer.WriteElementStringAsync(null, "UpdateId", null, supersededBy);
                }

                await writer.WriteEndElementAsync(); // SupersededBy
            }

            await writer.WriteEndElementAsync(); // Update
        }

        await writer.WriteEndElementAsync(); // Updates
        await writer.WriteEndDocumentAsync();
        await writer.FlushAsync();
    }

    private async Task CreateWuaCompatibilityFilesAsync(string filesDir, List<Update> updates, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating WUA compatibility files");

        // Create a simple metadata.xml file for WUA compatibility
        var metadataXmlPath = Path.Combine(filesDir, "metadata.xml");
        using (var fileStream = new FileStream(metadataXmlPath, FileMode.Create, FileAccess.Write))
        using (var writer = XmlWriter.Create(fileStream, new XmlWriterSettings
        {
            Async = true,
            Indent = true,
            Encoding = Encoding.UTF8
        }))
        {
            await writer.WriteStartDocumentAsync();
            await writer.WriteStartElementAsync(null, "Metadata", null);

            await writer.WriteElementStringAsync(null, "CreatedDate", null, DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"));
            await writer.WriteElementStringAsync(null, "UpdateCount", null, updates.Count.ToString());

            await writer.WriteEndElementAsync(); // Metadata
            await writer.WriteEndDocumentAsync();
            await writer.FlushAsync();
        }
    }



    private async Task UploadToMinioAsync(string filePath, string objectName, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Uploading CAB file to MinIO as {ObjectName}", objectName);

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(CabBucketName)
            .WithObject(objectName)
            .WithFileName(filePath)
            .WithContentType("application/octet-stream");

        await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

        _logger.LogInformation("CAB file uploaded to MinIO");
    }
}
