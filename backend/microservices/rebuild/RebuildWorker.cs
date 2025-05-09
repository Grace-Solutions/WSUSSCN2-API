namespace rebuild;

public class RebuildWorker : BackgroundService
{
    private readonly IRebuildService _rebuildService;
    private readonly ILogger<RebuildWorker> _logger;
    private readonly IConfiguration _configuration;

    public RebuildWorker(
        IRebuildService rebuildService,
        ILogger<RebuildWorker> logger,
        IConfiguration configuration)
    {
        _rebuildService = rebuildService;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Rebuild Worker started at: {time}", DateTimeOffset.Now);

        try
        {
            // Get grouping strategy from configuration
            var groupStrategy = _configuration["GROUP_STRATEGY"] ?? "Year-OS";
            _logger.LogInformation("Using group strategy: {strategy}", groupStrategy);

            // Check for pending rebuilds on startup
            await _rebuildService.RebuildCabsAsync(groupStrategy, stoppingToken);

            // Poll for updates that need new CABs
            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogInformation("Checking for updates that need new CABs at: {time}", DateTimeOffset.Now);
                await _rebuildService.RebuildCabsAsync(groupStrategy, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Rebuild Worker stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Rebuild Worker");
        }
    }
}
