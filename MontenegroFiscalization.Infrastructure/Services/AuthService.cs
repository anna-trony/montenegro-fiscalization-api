using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MontenegroFiscalization.Core.Services;
using MontenegroFiscalization.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace MontenegroFiscalization.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly FiscalizationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    
    public AuthService(
        FiscalizationDbContext context,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task<AuthResult> AuthenticateAsync(string apiKey)
    {
        try
        {
            var key = await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.Key == apiKey && k.IsActive);
            
            if (key == null)
            {
                return new AuthResult 
                { 
                    Success = false, 
                    ErrorMessage = "Invalid API key" 
                };
            }
            
            if (key.ExpiresAt.HasValue && key.ExpiresAt < DateTime.UtcNow)
            {
                return new AuthResult 
                { 
                    Success = false, 
                    ErrorMessage = "API key expired" 
                };
            }
            
            key.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            var tenant = await _context.TenantConfigurations
                .FirstOrDefaultAsync(t => t.TenantId == key.TenantId && t.IsActive);
            
            if (tenant == null)
            {
                return new AuthResult 
                { 
                    Success = false, 
                    ErrorMessage = "Tenant not active" 
                };
            }
            
            var token = GenerateJwtToken(tenant.TenantId, tenant.TenantName);
            
            return new AuthResult
            {
                Success = true,
                Token = token,
                TenantId = tenant.TenantId,
                TenantName = tenant.TenantName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for API key");
            return new AuthResult 
            { 
                Success = false, 
                ErrorMessage = "Authentication failed" 
            };
        }
    }
    
    public string GenerateJwtToken(string tenantId, string tenantName)
    {
        var key = Encoding.ASCII.GetBytes(_configuration["JWT:Secret"] ?? "your-256-bit-secret-key-for-jwt-token-generation-replace-this");
        var tokenHandler = new JwtSecurityTokenHandler();
        
        var claims = new List<Claim>
        {
            new Claim("tenant_id", tenantId),
            new Claim("tenant_name", tenantName),
            new Claim(ClaimTypes.Name, tenantName),
            new Claim("jti", Guid.NewGuid().ToString())
        };
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(24),
            Issuer = _configuration["JWT:Issuer"] ?? "montenegro-fiscalization-api",
            Audience = _configuration["JWT:Audience"] ?? "montenegro-fiscalization-clients",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    
    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        var key = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Key == apiKey && k.IsActive);
        
        if (key == null) return false;
        
        if (key.ExpiresAt.HasValue && key.ExpiresAt < DateTime.UtcNow)
            return false;
            
        return true;
    }
}