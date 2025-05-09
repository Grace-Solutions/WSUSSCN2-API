namespace parse;

public class ParseWorker : BackgroundService
{
    private readonly IParseService _parseService;
    private readonly ILogger<ParseWorker> _logger;

    public ParseWorker(
        IParseService parseService,
        ILogger<ParseWorker> logger)
    {
        _parseService = parseService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Parse Worker started at: {time}", DateTimeOffset.Now);

        try
        {
            // Check for unprocessed CABs on startup
            await _parseService.ProcessPendingCabsAsync(stoppingToken);

            // Poll for new CABs to process
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogInformation("Checking for new CABs to process at: {time}", DateTimeOffset.Now);
                await _parseService.ProcessPendingCabsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Parse Worker stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Parse Worker");
        }
    }
}
