using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MontenegroFiscalization.Core.Services;

namespace MontenegroFiscalization.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    /// <summary>
    /// Authenticating with API key and get JWT token
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> GetToken([FromBody] TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            return BadRequest(new { error = "API key is required" });
        }
        
        var result = await _authService.AuthenticateAsync(request.ApiKey);
        
        if (result.Success)
        {
            return Ok(new
            {
                token = result.Token,
                tenantId = result.TenantId,
                tenantName = result.TenantName,
                expiresIn = 86400 // 24 hours
            });
        }
        
        return Unauthorized(new { error = result.ErrorMessage });
    }
    
    /// <summary>
    /// Validating current JWT token
    /// </summary>
    [HttpGet("validate")]
    [Authorize]
    public IActionResult ValidateToken()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        var tenantName = User.FindFirst("tenant_name")?.Value;
        
        return Ok(new
        {
            valid = true,
            tenantId,
            tenantName,
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}

public class TokenRequest
{
    public string ApiKey { get; set; } = "";
}