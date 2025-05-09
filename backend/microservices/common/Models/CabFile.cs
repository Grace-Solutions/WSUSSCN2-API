namespace common.Models;

public class CabFile
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string GroupStrategy { get; set; } = string.Empty;
    public string GroupValue { get; set; } = string.Empty;
    public string MinioPath { get; set; } = string.Empty;
    public long? SizeBytes { get; set; }
    public int? UpdateCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
