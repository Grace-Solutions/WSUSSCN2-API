using System.Text.RegularExpressions;

namespace sync;

public class SyncWorker : BackgroundService
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncWorker> _logger;
    private readonly IConfiguration _configuration;

    public SyncWorker(
        ISyncService syncService,
        ILogger<SyncWorker> logger,
        IConfiguration configuration)
    {
        _syncService = syncService;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sync Worker started at: {time}", DateTimeOffset.Now);

        try
        {
            // Run initial sync on startup
            await _syncService.SyncAsync(stoppingToken);

            // Parse sync interval from configuration
            var syncIntervalStr = _configuration["CAB_SYNC_INTERVAL"] ?? "1d";
            var interval = ParseHumanReadableDuration(syncIntervalStr);

            _logger.LogInformation("Sync interval set to {interval}", syncIntervalStr);

            // Schedule periodic sync
            using var timer = new PeriodicTimer(interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogInformation("Running scheduled sync at: {time}", DateTimeOffset.Now);
                await _syncService.SyncAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Sync Worker stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Sync Worker");
        }
    }

    private static TimeSpan ParseHumanReadableDuration(string duration)
    {
        // Parse human-readable durations like "1d12h30m"
        var result = TimeSpan.Zero;
        var regex = new Regex(@"(\d+)([dhms])");
        var matches = regex.Matches(duration.ToLower());

        foreach (Match match in matches)
        {
            var value = int.Parse(match.Groups[1].Value);
            var unit = match.Groups[2].Value;

            result += unit switch
            {
                "d" => TimeSpan.FromDays(value),
                "h" => TimeSpan.FromHours(value),
                "m" => TimeSpan.FromMinutes(value),
                "s" => TimeSpan.FromSeconds(value),
                _ => TimeSpan.Zero
            };
        }

        // Default to 1 day if parsing fails
        return result == TimeSpan.Zero ? TimeSpan.FromDays(1) : result;
    }
}
