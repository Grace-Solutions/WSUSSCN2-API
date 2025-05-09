using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CabsController : ControllerBase
{
    private readonly ICabService _cabService;
    private readonly ILogger<CabsController> _logger;

    public CabsController(ICabService cabService, ILogger<CabsController> logger)
    {
        _cabService = cabService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Policy = "UpdatesRead")]
    public async Task<ActionResult<IEnumerable<CabFileDto>>> GetCabFiles()
    {
        try
        {
            var cabFiles = await _cabService.GetCabFilesAsync();
            return Ok(cabFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CAB files");
            return StatusCode(500, "An error occurred while retrieving CAB files");
        }
    }

    [HttpGet("{group}")]
    [Authorize(Policy = "CabsRead")]
    public async Task<ActionResult<string>> GetCabFileUrl(string group)
    {
        try
        {
            // Parse group parameter (format: strategy-value)
            var parts = group.Split('-', 2);
            if (parts.Length != 2)
            {
                return BadRequest("Invalid group format. Expected: strategy-value");
            }

            var strategy = parts[0];
            var value = parts[1];

            var cabFile = await _cabService.GetCabFileByGroupAsync(strategy, value);
            if (cabFile == null)
            {
                return NotFound();
            }

            var url = await _cabService.GetPresignedCabUrlAsync(cabFile.Id);
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CAB file URL for group {Group}", group);
            return StatusCode(500, "An error occurred while retrieving the CAB file URL");
        }
    }

    [HttpGet("source")]
    [Authorize(Policy = "UpdatesRead")]
    public async Task<ActionResult<string>> GetSourceCabUrl()
    {
        try
        {
            var url = await _cabService.GetPresignedSourceCabUrlAsync();
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting source CAB URL");
            return StatusCode(500, "An error occurred while retrieving the source CAB URL");
        }
    }
}
