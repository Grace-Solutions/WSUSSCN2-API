using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(ISyncService syncService, ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    [HttpPost("trigger")]
    [Authorize(Policy = "SyncTrigger")]
    public async Task<ActionResult<SyncStatusDto>> TriggerSync()
    {
        try
        {
            var status = await _syncService.TriggerSyncAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering sync");
            return StatusCode(500, "An error occurred while triggering sync");
        }
    }

    [HttpGet("history")]
    [Authorize(Policy = "UpdatesRead")]
    public async Task<ActionResult<IEnumerable<SyncHistoryDto>>> GetSyncHistory()
    {
        try
        {
            var history = await _syncService.GetSyncHistoryAsync();
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sync history");
            return StatusCode(500, "An error occurred while retrieving sync history");
        }
    }
}
