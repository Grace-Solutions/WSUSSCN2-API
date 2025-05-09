namespace common.Models;

public class Update
{
    public Guid Id { get; set; }
    public string UpdateId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Classification { get; set; }
    public string? Product { get; set; }
    public string? ProductFamily { get; set; }
    public string? KbArticleId { get; set; }
    public string? SecurityBulletinId { get; set; }
    public string? MsrcSeverity { get; set; }
    public string[]? Categories { get; set; }
    public bool IsSuperseded { get; set; }
    public string[]? SupersededBy { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public DateTime? LastModified { get; set; }
    public string? OsVersion { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
