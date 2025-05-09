using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace api.Auth;

public class TokenService : ITokenService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TokenService> _logger;

    public TokenService(AppDbContext dbContext, ILogger<TokenService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiToken?> ValidateTokenAsync(string token)
    {
        var apiToken = await _dbContext.ApiTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Token == token);

        return apiToken != null ? MapToApiToken(apiToken) : null;
    }

    public async Task<IEnumerable<ApiToken>> GetAllTokensAsync()
    {
        var tokens = await _dbContext.ApiTokens
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tokens.Select(MapToApiToken);
    }

    public async Task<ApiToken?> GetTokenByIdAsync(Guid id)
    {
        var token = await _dbContext.ApiTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);

        return token != null ? MapToApiToken(token) : null;
    }

    public async Task<ApiToken> CreateTokenAsync(ApiTokenCreateRequest request, string createdBy)
    {
        var token = GenerateToken();
        
        var apiToken = new Models.ApiToken
        {
            Token = token,
            Label = request.Label,
            Description = request.Description,
            Permissions = request.Permissions.ToArray(),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = request.ExpiresAt
        };

        _dbContext.ApiTokens.Add(apiToken);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created token {Id} with label {Label}", apiToken.Id, apiToken.Label);

        return MapToApiToken(apiToken);
    }

    public async Task<ApiToken?> UpdateTokenAsync(Guid id, ApiTokenUpdateRequest request, string modifiedBy)
    {
        var apiToken = await _dbContext.ApiTokens.FindAsync(id);
        if (apiToken == null)
        {
            return null;
        }

        if (request.Label != null)
        {
            apiToken.Label = request.Label;
        }

        if (request.Description != null)
        {
            apiToken.Description = request.Description;
        }

        if (request.Permissions != null)
        {
            apiToken.Permissions = request.Permissions.ToArray();
        }

        if (request.ExpiresAt != null)
        {
            apiToken.ExpiresAt = request.ExpiresAt;
        }

        apiToken.LastModifiedBy = modifiedBy;
        apiToken.LastModifiedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated token {Id}", apiToken.Id);

        return MapToApiToken(apiToken);
    }

    public async Task<bool> RevokeTokenAsync(Guid id, string revokedBy)
    {
        var apiToken = await _dbContext.ApiTokens.FindAsync(id);
        if (apiToken == null)
        {
            return false;
        }

        apiToken.Revoked = true;
        apiToken.RevokedAt = DateTime.UtcNow;
        apiToken.LastModifiedBy = revokedBy;
        apiToken.LastModifiedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Revoked token {Id}", apiToken.Id);

        return true;
    }

    private static string GenerateToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static ApiToken MapToApiToken(Models.ApiToken token)
    {
        return new ApiToken
        {
            Id = token.Id,
            Token = token.Token,
            Label = token.Label,
            Description = token.Description,
            Permissions = token.Permissions?.ToList() ?? new List<string>(),
            CreatedBy = token.CreatedBy,
            CreatedAt = token.CreatedAt,
            LastModifiedBy = token.LastModifiedBy,
            LastModifiedAt = token.LastModifiedAt,
            Revoked = token.Revoked,
            RevokedAt = token.RevokedAt,
            ExpiresAt = token.ExpiresAt
        };
    }
}
