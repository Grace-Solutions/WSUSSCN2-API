namespace api.Auth;

public interface ITokenService
{
    Task<ApiToken?> ValidateTokenAsync(string token);
    Task<IEnumerable<ApiToken>> GetAllTokensAsync();
    Task<ApiToken?> GetTokenByIdAsync(Guid id);
    Task<ApiToken> CreateTokenAsync(ApiTokenCreateRequest request, string createdBy);
    Task<ApiToken?> UpdateTokenAsync(Guid id, ApiTokenUpdateRequest request, string modifiedBy);
    Task<bool> RevokeTokenAsync(Guid id, string revokedBy);
}

public class ApiToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public bool Revoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class ApiTokenCreateRequest
{
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
}

public class ApiTokenUpdateRequest
{
    public string? Label { get; set; }
    public string? Description { get; set; }
    public List<string>? Permissions { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
