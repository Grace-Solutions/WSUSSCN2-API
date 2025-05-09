namespace sync;

public interface ISyncService
{
    Task SyncAsync(CancellationToken cancellationToken);
}
