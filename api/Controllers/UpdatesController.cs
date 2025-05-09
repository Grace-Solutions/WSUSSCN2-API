using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "UpdatesRead")]
public class UpdatesController : ControllerBase
{
    private readonly IUpdateService _updateService;
    private readonly ILogger<UpdatesController> _logger;

    public UpdatesController(IUpdateService updateService, ILogger<UpdatesController> logger)
    {
        _updateService = updateService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UpdateDto>>> GetUpdates([FromQuery] UpdateFilter filter)
    {
        try
        {
            var updates = await _updateService.GetUpdatesAsync(filter);
            return Ok(updates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting updates");
            return StatusCode(500, "An error occurred while retrieving updates");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UpdateDto>> GetUpdate(Guid id)
    {
        try
        {
            var update = await _updateService.GetUpdateByIdAsync(id);
            if (update == null)
            {
                return NotFound();
            }

            return Ok(update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting update {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the update");
        }
    }

    [HttpGet("changed-since")]
    public async Task<ActionResult<IEnumerable<UpdateDto>>> GetUpdatesChangedSince([FromQuery] DateTime since)
    {
        try
        {
            var updates = await _updateService.GetUpdatesChangedSinceAsync(since);
            return Ok(updates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting updates changed since {Since}", since);
            return StatusCode(500, "An error occurred while retrieving updates");
        }
    }
}
