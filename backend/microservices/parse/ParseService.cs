using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Text;
using System.Xml;
using DotNetCab;

namespace parse;

public class ParseService : IParseService
{
    private readonly AppDbContext _dbContext;
    private readonly IMinioClient _minioClient;
    private readonly ILogger<ParseService> _logger;
    private const string SourceBucketName = "source-cabs";

    public ParseService(
        AppDbContext dbContext,
        IMinioClient minioClient,
        ILogger<ParseService> logger)
    {
        _dbContext = dbContext;
        _minioClient = minioClient;
        _logger = logger;
    }

    public async Task ProcessPendingCabsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for pending CABs to process");

        var pendingCabs = await _dbContext.SourceCabs
            .Where(c => !c.Processed)
            .OrderBy(c => c.DownloadedAt)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} pending CABs to process", pendingCabs.Count);

        foreach (var cab in pendingCabs)
        {
            await ProcessCabAsync(cab.Id, cancellationToken);
        }
    }

    public async Task ProcessCabAsync(Guid sourceCabId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing CAB {Id}", sourceCabId);

        try
        {
            // Get source cab
            var sourceCab = await _dbContext.SourceCabs
                .FirstOrDefaultAsync(c => c.Id == sourceCabId, cancellationToken);

            if (sourceCab == null)
            {
                _logger.LogWarning("Source CAB {Id} not found", sourceCabId);
                return;
            }

            // Create sync history record
            var syncHistory = new SyncHistory
            {
                StartedAt = DateTime.UtcNow,
                Status = "Processing",
                SourceCabId = sourceCabId
            };

            _dbContext.SyncHistory.Add(syncHistory);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Download from MinIO
            var tempDir = Path.Combine(Path.GetTempPath(), "wsusscn2-api", sourceCabId.ToString());
            Directory.CreateDirectory(tempDir);
            var cabPath = Path.Combine(tempDir, sourceCab.FileName);

            await DownloadFromMinioAsync(sourceCab.MinioPath, cabPath, cancellationToken);

            // Extract and process CAB
            var (updatesAdded, updatesModified) = await ExtractAndProcessCabAsync(cabPath, cancellationToken);

            // Update source cab
            sourceCab.Processed = true;
            sourceCab.ProcessedAt = DateTime.UtcNow;

            // Update sync history
            syncHistory.Status = "Completed";
            syncHistory.CompletedAt = DateTime.UtcNow;
            syncHistory.UpdatesAdded = updatesAdded;
            syncHistory.UpdatesModified = updatesModified;

            await _dbContext.SaveChangesAsync(cancellationToken);

            // Clean up
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up temp directory {TempDir}", tempDir);
            }

            _logger.LogInformation("CAB {Id} processed successfully. Added: {Added}, Modified: {Modified}",
                sourceCabId, updatesAdded, updatesModified);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CAB {Id}", sourceCabId);

            // Update sync history with error
            var syncHistory = await _dbContext.SyncHistory
                .Where(s => s.SourceCabId == sourceCabId)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (syncHistory != null)
            {
                syncHistory.Status = "Failed";
                syncHistory.CompletedAt = DateTime.UtcNow;
                syncHistory.ErrorMessage = ex.Message;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            throw;
        }
    }

    private async Task DownloadFromMinioAsync(string objectName, string filePath, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Downloading {ObjectName} from MinIO", objectName);

        var getObjectArgs = new GetObjectArgs()
            .WithBucket(SourceBucketName)
            .WithObject(objectName)
            .WithFile(filePath);

        await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);

        _logger.LogInformation("Downloaded {ObjectName} to {FilePath}", objectName, filePath);
    }

    private async Task<(int UpdatesAdded, int UpdatesModified)> ExtractAndProcessCabAsync(string cabPath, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Extracting and processing CAB {CabPath} using streaming approach", cabPath);

        int updatesAdded = 0;
        int updatesModified = 0;

        try
        {
            // Process the CAB file with streaming
            using var cabinet = new Cabinet(cabPath);
            var entries = cabinet.GetEntries();

            // First process index.xml
            var indexEntry = entries.FirstOrDefault(e => e.Name.Equals("index.xml", StringComparison.OrdinalIgnoreCase));
            if (indexEntry != null)
            {
                _logger.LogInformation("Processing index.xml from main CAB");
                using var indexStream = cabinet.OpenEntryStream(indexEntry.Name);
                var (added, modified) = await ProcessIndexXmlStreamAsync(indexStream, cancellationToken);
                updatesAdded += added;
                updatesModified += modified;
            }
            else
            {
                _logger.LogWarning("index.xml not found in CAB file");
            }

            // Then process nested CABs
            var nestedCabEntries = entries.Where(e =>
                e.Name.StartsWith("package/", StringComparison.OrdinalIgnoreCase) &&
                e.Name.EndsWith(".cab", StringComparison.OrdinalIgnoreCase)).ToList();

            _logger.LogInformation("Found {Count} nested CAB files", nestedCabEntries.Count);

            foreach (var entry in nestedCabEntries)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                _logger.LogInformation("Processing nested CAB: {Name}", entry.Name);

                // For nested CABs, we need to save to a temporary file first
                var tempNestedCabPath = Path.Combine(Path.GetTempPath(), "wsusscn2-api", "nested", Path.GetFileName(entry.Name));
                Directory.CreateDirectory(Path.GetDirectoryName(tempNestedCabPath));

                try
                {
                    using (var entryStream = cabinet.OpenEntryStream(entry.Name))
                    using (var fileStream = new FileStream(tempNestedCabPath, FileMode.Create, FileAccess.Write))
                    {
                        await entryStream.CopyToAsync(fileStream, cancellationToken);
                    }

                    // Process the nested CAB
                    var (added, modified) = await ProcessNestedCabAsync(tempNestedCabPath, cancellationToken);
                    updatesAdded += added;
                    updatesModified += modified;
                }
                finally
                {
                    // Clean up
                    try
                    {
                        if (File.Exists(tempNestedCabPath))
                            File.Delete(tempNestedCabPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error deleting temporary nested CAB file {Path}", tempNestedCabPath);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CAB file {CabPath}", cabPath);
            throw;
        }

        _logger.LogInformation("Processed CAB. Added: {Added}, Modified: {Modified}", updatesAdded, updatesModified);

        return (updatesAdded, updatesModified);
    }

    private async Task<(int UpdatesAdded, int UpdatesModified)> ProcessNestedCabAsync(string nestedCabPath, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing nested CAB {Path}", nestedCabPath);

        int updatesAdded = 0;
        int updatesModified = 0;

        try
        {
            using var cabinet = new Cabinet(nestedCabPath);
            var entries = cabinet.GetEntries();

            foreach (var entry in entries)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Process XML files directly from stream
                if (entry.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Processing XML file {FileName} from nested CAB", entry.Name);

                    using var entryStream = cabinet.OpenEntryStream(entry.Name);
                    var (added, modified) = await ProcessXmlStreamAsync(entry.Name, entryStream, cancellationToken);
                    updatesAdded += added;
                    updatesModified += modified;
                }
            }

            return (updatesAdded, updatesModified);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing nested CAB {Path}", nestedCabPath);
            throw;
        }
    }

    private async Task<(int UpdatesAdded, int UpdatesModified)> ProcessXmlStreamAsync(string fileName, Stream xmlStream, CancellationToken cancellationToken)
    {
        // Different processing based on file name or content
        if (fileName.Contains("metadata", StringComparison.OrdinalIgnoreCase))
        {
            return await ProcessMetadataXmlStreamAsync(xmlStream, cancellationToken);
        }
        else if (fileName.Contains("package", StringComparison.OrdinalIgnoreCase))
        {
            return await ProcessPackageXmlStreamAsync(xmlStream, cancellationToken);
        }
        else
        {
            // Default to processing as an update XML
            return await ProcessUpdateXmlStreamAsync(xmlStream, cancellationToken);
        }
    }

    private async Task<(int UpdatesAdded, int UpdatesModified)> ProcessIndexXmlStreamAsync(Stream xmlStream, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing Index.xml using streaming approach");

        int updatesAdded = 0;
        int updatesModified = 0;

        try
        {
            // Create XmlReader directly from the stream
            using var reader = XmlReader.Create(xmlStream, new XmlReaderSettings
            {
                Async = true,
                IgnoreWhitespace = true,
                IgnoreComments = true
            });

            // Move to the root element
            await reader.MoveToContentAsync();

            // Process updates
            while (await reader.ReadAsync())
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Update")
                {
                    // Process a single update
                    var (added, modified) = await ProcessUpdateElementAsync(reader, cancellationToken);
                    updatesAdded += added;
                    updatesModified += modified;

                    // Save periodically to avoid large transactions
                    if ((updatesAdded + updatesModified) % 50 == 0)
                    {
                        await _dbContext.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Saved batch of updates. Added: {Added}, Modified: {Modified}",
                            updatesAdded, updatesModified);
                    }
                }
            }

            // Final save
            if ((updatesAdded + updatesModified) > 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return (updatesAdded, updatesModified);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Index.xml");
            throw;
        }
    }

    private async Task<(int UpdatesAdded, int UpdatesModified)> ProcessMetadataXmlStreamAsync(Stream xmlStream, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing metadata XML using streaming");

        // Create XmlReader directly from the stream
        using var reader = XmlReader.Create(xmlStream, new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true
        });

        // Process metadata XML - this is a simplified implementation
        // In a real implementation, this would parse the metadata XML structure

        int updatesAdded = 0;
        int updatesModified = 0;

        // Move to the root element
        await reader.MoveToContentAsync();

        // Process metadata elements
        while (await reader.ReadAsync())
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            // Process metadata elements as needed
        }

        return (updatesAdded, updatesModified);
    }

    private async Task<(int UpdatesAdded, int UpdatesModified)> ProcessPackageXmlStreamAsync(Stream xmlStream, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing package XML using streaming");

        // Create XmlReader directly from the stream
        using var reader = XmlReader.Create(xmlStream, new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true
        });

        // Process package XML - this is a simplified implementation
        // In a real implementation, this would parse the package XML structure

        int updatesAdded = 0;
        int updatesModified = 0;

        // Move to the root element
        await reader.MoveToContentAsync();

        // Process package elements
        while (await reader.ReadAsync())
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (reader.NodeType == XmlNodeType.Element && reader.Name == "Package")
            {
                // Process package element
                // This is a placeholder for actual package processing
            }
        }

        return (updatesAdded, updatesModified);
    }

    private async Task<(int UpdatesAdded, int UpdatesModified)> ProcessUpdateXmlStreamAsync(Stream xmlStream, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing update XML using streaming");

        // Create XmlReader directly from the stream
        using var reader = XmlReader.Create(xmlStream, new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true
        });

        // Process update XML - this is a simplified implementation
        // In a real implementation, this would parse the update XML structure

        int updatesAdded = 0;
        int updatesModified = 0;

        // Move to the root element
        await reader.MoveToContentAsync();

        // Process update elements
        while (await reader.ReadAsync())
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (reader.NodeType == XmlNodeType.Element && reader.Name == "Update")
            {
                var (added, modified) = await ProcessUpdateElementAsync(reader, cancellationToken);
                updatesAdded += added;
                updatesModified += modified;
            }
        }

        // Final save
        if ((updatesAdded + updatesModified) > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return (updatesAdded, updatesModified);
    }
}
