using api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Admin")]
public class TokensController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<TokensController> _logger;

    public TokensController(ITokenService tokenService, ILogger<TokensController> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiToken>>> GetTokens()
    {
        try
        {
            var tokens = await _tokenService.GetAllTokensAsync();
            return Ok(tokens);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tokens");
            return StatusCode(500, "An error occurred while retrieving tokens");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiToken>> GetToken(Guid id)
    {
        try
        {
            var token = await _tokenService.GetTokenByIdAsync(id);
            if (token == null)
            {
                return NotFound();
            }

            return Ok(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting token {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the token");
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiToken>> CreateToken(ApiTokenCreateRequest request)
    {
        try
        {
            var username = User.FindFirstValue(ClaimTypes.Name) ?? "system";
            var token = await _tokenService.CreateTokenAsync(request, username);
            return CreatedAtAction(nameof(GetToken), new { id = token.Id }, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating token");
            return StatusCode(500, "An error occurred while creating the token");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiToken>> UpdateToken(Guid id, ApiTokenUpdateRequest request)
    {
        try
        {
            var username = User.FindFirstValue(ClaimTypes.Name) ?? "system";
            var token = await _tokenService.UpdateTokenAsync(id, request, username);
            if (token == null)
            {
                return NotFound();
            }

            return Ok(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating token {Id}", id);
            return StatusCode(500, "An error occurred while updating the token");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> RevokeToken(Guid id)
    {
        try
        {
            var username = User.FindFirstValue(ClaimTypes.Name) ?? "system";
            var result = await _tokenService.RevokeTokenAsync(id, username);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token {Id}", id);
            return StatusCode(500, "An error occurred while revoking the token");
        }
    }
}
