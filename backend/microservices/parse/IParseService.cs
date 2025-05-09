namespace parse;

public interface IParseService
{
    Task ProcessPendingCabsAsync(CancellationToken cancellationToken);
    Task ProcessCabAsync(Guid sourceCabId, CancellationToken cancellationToken);
}
