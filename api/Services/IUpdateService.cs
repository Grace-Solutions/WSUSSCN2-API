namespace api.Services;

public interface IUpdateService
{
    Task<IEnumerable<UpdateDto>> GetUpdatesAsync(UpdateFilter filter);
    Task<UpdateDto?> GetUpdateByIdAsync(Guid id);
    Task<IEnumerable<UpdateDto>> GetUpdatesChangedSinceAsync(DateTime since);
}

public class UpdateDto
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
    public List<string>? Categories { get; set; }
    public bool IsSuperseded { get; set; }
    public List<string>? SupersededBy { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public DateTime? LastModified { get; set; }
    public string? OsVersion { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
}

public class UpdateFilter
{
    public string? Product { get; set; }
    public string? ProductFamily { get; set; }
    public string? Classification { get; set; }
    public string? Category { get; set; }
    public string? OsVersion { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public DateTime? ReleasedAfter { get; set; }
    public DateTime? ReleasedBefore { get; set; }
    public bool? IsSuperseded { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
