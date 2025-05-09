namespace common.Models;

public class SyncHistory
{
    public Guid Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Status { get; set; }
    public Guid? SourceCabId { get; set; }
    public int UpdatesAdded { get; set; }
    public int UpdatesModified { get; set; }
    public string? ErrorMessage { get; set; }
}
