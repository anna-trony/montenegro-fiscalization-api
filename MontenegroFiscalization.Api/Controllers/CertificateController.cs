using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MontenegroFiscalization.Core.Services;

namespace MontenegroFiscalization.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CertificateController : ControllerBase
{
    private readonly ICertificateService _certificateService;
    private readonly ILogger<CertificateController> _logger;
    
    public CertificateController(
        ICertificateService certificateService,
        ILogger<CertificateController> logger)
    {
        _certificateService = certificateService;
        _logger = logger;
    }
    
    /// <summary>
    /// Uploading certificate for tenant
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadCertificate([FromForm] CertificateUploadRequest request)
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(new { error = "Tenant ID not found" });
        }
        
        if (request.Certificate == null || request.Certificate.Length == 0)
        {
            return BadRequest(new { error = "Certificate file is required" });
        }
        
        using var ms = new MemoryStream();
        await request.Certificate.CopyToAsync(ms);
        var certData = ms.ToArray();
        
        var result = await _certificateService.StoreCertificateAsync(
            tenantId, 
            certData, 
            request.Password ?? ""
        );
        
        if (result)
        {
            _logger.LogInformation("Certificate uploaded for tenant {TenantId}", tenantId);
            return Ok(new { message = "Certificate uploaded successfully" });
        }
        
        return BadRequest(new { error = "Failed to upload certificate" });
    }
    
    /// <summary>
    /// Getting certificate information
    /// </summary>
    [HttpGet("info")]
    public async Task<IActionResult> GetCertificateInfo()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(new { error = "Tenant ID not found" });
        }
        
        var info = await _certificateService.GetCertificateInfoAsync(tenantId);
        
        if (info == null)
        {
            return NotFound(new { error = "Certificate not found" });
        }
        
        return Ok(info);
    }
    
    /// <summary>
    /// Validating certificate
    /// </summary>
    [HttpGet("validate")]
    public async Task<IActionResult> ValidateCertificate()
    {
        var tenantId = User.FindFirst("tenant_id")?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(new { error = "Tenant ID not found" });
        }
        
        var isValid = await _certificateService.ValidateCertificateAsync(tenantId);
        
        return Ok(new { valid = isValid });
    }
}

public class CertificateUploadRequest
{
    public IFormFile? Certificate { get; set; }
    public string? Password { get; set; }
}