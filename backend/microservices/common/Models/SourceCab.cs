namespace common.Models;

public class SourceCab
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string MinioPath { get; set; } = string.Empty;
    public string? Etag { get; set; }
    public long? SizeBytes { get; set; }
    public DateTime DownloadedAt { get; set; }
    public bool Processed { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
