namespace MontenegroFiscalization.Core.Services;

public interface IAuthService
{
    Task<AuthResult> AuthenticateAsync(string apiKey);
    string GenerateJwtToken(string tenantId, string tenantName);
    Task<bool> ValidateApiKeyAsync(string apiKey);
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? TenantId { get; set; }
    public string? TenantName { get; set; }
    public string? ErrorMessage { get; set; }
}