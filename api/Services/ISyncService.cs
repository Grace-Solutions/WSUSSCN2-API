namespace api.Services;

public interface ISyncService
{
    Task<SyncStatusDto> TriggerSyncAsync();
    Task<IEnumerable<SyncHistoryDto>> GetSyncHistoryAsync();
}

public class SyncStatusDto
{
    public Guid SyncId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
}

public class SyncHistoryDto
{
    public Guid Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int UpdatesAdded { get; set; }
    public int UpdatesModified { get; set; }
    public string? ErrorMessage { get; set; }
}
