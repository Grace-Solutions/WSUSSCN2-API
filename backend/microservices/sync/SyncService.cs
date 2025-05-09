using Microsoft.EntityFrameworkCore;
using Minio;
using Minio.DataModel.Args;

namespace sync;

public class SyncService : ISyncService
{
    private readonly AppDbContext _dbContext;
    private readonly IMinioClient _minioClient;
    private readonly ILogger<SyncService> _logger;
    private const string SourceUrl = "http://download.windowsupdate.com/microsoftupdate/v6/wsusscan/wsusscn2.cab";
    private const string BucketName = "source-cabs";

    public SyncService(
        AppDbContext dbContext,
        IMinioClient minioClient,
        ILogger<SyncService> logger)
    {
        _dbContext = dbContext;
        _minioClient = minioClient;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting sync process");

        try
        {
            // Create sync history record
            var syncHistory = new SyncHistory
            {
                StartedAt = DateTime.UtcNow,
                Status = "Started"
            };

            _dbContext.SyncHistory.Add(syncHistory);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Ensure bucket exists
            await EnsureBucketExistsAsync(cancellationToken);

            // Download the CAB file
            var (cabPath, etag) = await DownloadCabFileAsync(cancellationToken);

            // Check if we already have this version
            var existingCab = await _dbContext.SourceCabs
                .OrderByDescending(c => c.DownloadedAt)
                .FirstOrDefaultAsync(c => c.Etag == etag, cancellationToken);

            if (existingCab != null)
            {
                _logger.LogInformation("CAB file with ETag {ETag} already exists, skipping", etag);
                
                syncHistory.Status = "Skipped";
                syncHistory.CompletedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);
                
                return;
            }

            // Upload to MinIO
            var objectName = $"wsusscn2_{DateTime.UtcNow:yyyyMMdd_HHmmss}.cab";
            await UploadToMinioAsync(cabPath, objectName, cancellationToken);

            // Get file size
            var fileInfo = new FileInfo(cabPath);
            var fileSize = fileInfo.Length;

            // Create source cab record
            var sourceCab = new SourceCab
            {
                FileName = objectName,
                MinioPath = objectName,
                Etag = etag,
                SizeBytes = fileSize,
                DownloadedAt = DateTime.UtcNow,
                Processed = false
            };

            _dbContext.SourceCabs.Add(sourceCab);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Update sync history
            syncHistory.Status = "Completed";
            syncHistory.CompletedAt = DateTime.UtcNow;
            syncHistory.SourceCabId = sourceCab.Id;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Sync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync process");

            // Update sync history with error
            var syncHistory = await _dbContext.SyncHistory
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

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var bucketExistsArgs = new BucketExistsArgs().WithBucket(BucketName);
        var exists = await _minioClient.BucketExistsAsync(bucketExistsArgs, cancellationToken);
        if (!exists)
        {
            _logger.LogInformation("Creating bucket {BucketName}", BucketName);
            var makeBucketArgs = new MakeBucketArgs().WithBucket(BucketName);
            await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);
        }
    }

    private async Task<(string FilePath, string ETag)> DownloadCabFileAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Downloading CAB file from {Url}", SourceUrl);

        // Create temp directory
        var tempDir = Path.Combine(Path.GetTempPath(), "wsusscn2-api");
        Directory.CreateDirectory(tempDir);

        // Download file
        var tempFile = Path.Combine(tempDir, "wsusscn2.cab");
        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(SourceUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var etag = response.Headers.ETag?.Tag ?? Guid.NewGuid().ToString();

        using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fileStream, cancellationToken);

        _logger.LogInformation("CAB file downloaded to {FilePath}", tempFile);

        return (tempFile, etag);
    }

    private async Task UploadToMinioAsync(string filePath, string objectName, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Uploading CAB file to MinIO as {ObjectName}", objectName);

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName)
            .WithFileName(filePath)
            .WithContentType("application/octet-stream");

        await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

        _logger.LogInformation("CAB file uploaded to MinIO");
    }
}
