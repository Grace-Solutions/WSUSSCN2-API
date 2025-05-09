namespace api.Services;

public interface ICabService
{
    Task<IEnumerable<CabFileDto>> GetCabFilesAsync();
    Task<CabFileDto?> GetCabFileByGroupAsync(string groupStrategy, string groupValue);
    Task<string> GetPresignedCabUrlAsync(Guid cabId);
    Task<string> GetPresignedSourceCabUrlAsync();
}

public class CabFileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string GroupStrategy { get; set; } = string.Empty;
    public string GroupValue { get; set; } = string.Empty;
    public long? SizeBytes { get; set; }
    public int? UpdateCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
