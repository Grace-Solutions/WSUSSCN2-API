namespace rebuild;

public interface IRebuildService
{
    Task RebuildCabsAsync(string groupStrategy, CancellationToken cancellationToken);
}
