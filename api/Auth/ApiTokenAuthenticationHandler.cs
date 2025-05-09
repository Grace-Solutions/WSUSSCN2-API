using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace api.Auth;

public class ApiTokenAuthenticationOptions : AuthenticationSchemeOptions
{
}

public class ApiTokenAuthenticationHandler : AuthenticationHandler<ApiTokenAuthenticationOptions>
{
    private readonly ITokenService _tokenService;
    private readonly ILogger<ApiTokenAuthenticationHandler> _logger;

    public ApiTokenAuthenticationHandler(
        IOptionsMonitor<ApiTokenAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        ITokenService tokenService) : base(options, logger, encoder, clock)
    {
        _tokenService = tokenService;
        _logger = logger.CreateLogger<ApiTokenAuthenticationHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Skip authentication if header is not present
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.NoResult();
        }

        var authorizationHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authorizationHeader))
        {
            return AuthenticateResult.NoResult();
        }

        // Extract token from header
        string token;
        if (authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = authorizationHeader.Substring("Bearer ".Length).Trim();
        }
        else
        {
            token = authorizationHeader.Trim();
        }

        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            // Validate token
            var apiToken = await _tokenService.ValidateTokenAsync(token);
            if (apiToken == null)
            {
                _logger.LogWarning("Invalid token: {Token}", token);
                return AuthenticateResult.Fail("Invalid token");
            }

            if (apiToken.Revoked)
            {
                _logger.LogWarning("Revoked token: {Token}", token);
                return AuthenticateResult.Fail("Token has been revoked");
            }

            if (apiToken.ExpiresAt.HasValue && apiToken.ExpiresAt.Value < DateTime.UtcNow)
            {
                _logger.LogWarning("Expired token: {Token}", token);
                return AuthenticateResult.Fail("Token has expired");
            }

            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, apiToken.Id.ToString()),
                new Claim(ClaimTypes.Name, apiToken.Label)
            };

            // Add permission claims
            foreach (var permission in apiToken.Permissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while validating token");
            return AuthenticateResult.Fail("An error occurred while validating the token");
        }
    }
}
