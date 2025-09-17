using Microsoft.AspNetCore.Mvc;
using MontenegroFiscalization.Core.Interfaces;
using MontenegroFiscalization.Api.DTOs;
using MontenegroFiscalization.Api.Services;

namespace MontenegroFiscalization.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class FiscalizationController : ControllerBase
{
    private readonly IFiscalizationService _fiscalizationService;
    private readonly IMappingService _mappingService;
    private readonly ILogger<FiscalizationController> _logger;
    
    public FiscalizationController(
        IFiscalizationService fiscalizationService,
        IMappingService mappingService,
        ILogger<FiscalizationController> logger)
    {
        _fiscalizationService = fiscalizationService;
        _mappingService = mappingService;
        _logger = logger;
    }
    
    /// <summary>
    /// Fiscalizing an invoice
    /// </summary>
    [HttpPost("invoice")]
    public async Task<IActionResult> FiscalizeInvoice([FromBody] InvoiceRequest request)
    {
        _logger.LogInformation("Fiscalizing invoice {Number}", request.Number);
        
        if (request.TotalPrice <= 0)
        {
            return BadRequest(new FiscalizationResponse
            {
                Success = false,
                Message = "Total price must be greater than 0",
                Errors = new List<string> { "Invalid invoice amount" }
            });
        }
        
        try
        {
            var invoice = _mappingService.MapToInvoice(request);
            
            var tenantId = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized(new { error = "Tenant ID not found in token" });
            }
            
            var result = await _fiscalizationService.FiscalizeInvoiceAsync(invoice, tenantId);
            
            if (result.Success)
            {
                return Ok(new FiscalizationResponse
                {
                    Success = true,
                    FIC = result.FIC,
                    UUID = result.UUID,
                    IIC = invoice.IIC,
                    IICSignature = invoice.IICSignature,
                    Message = "Invoice fiscalized successfully",
                    QRCodeUrl = _mappingService.GenerateQRCodeUrl(invoice.IIC, result.FIC),
                    InvoiceNumber = _mappingService.FormatInvoiceNumber(invoice)
                });
            }
            
            _logger.LogError("Fiscalization failed: {Errors}", string.Join(", ", result.Errors));
            return BadRequest(new FiscalizationResponse
            {
                Success = false,
                Message = "Fiscalization failed",
                Errors = result.Errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during fiscalization");
            return StatusCode(500, new FiscalizationResponse
            {
                Success = false,
                Message = "Internal server error",
                Errors = new List<string> { "An unexpected error occurred" }
            });
        }
    }
    
    /// <summary>
    /// Registering cash deposit
    /// </summary>
    [HttpPost("deposit")]
    public async Task<IActionResult> RegisterDeposit([FromBody] DepositRequest request)
    {
        _logger.LogInformation("Registering deposit: {Operation} {Amount}", request.Operation, request.Amount);
        
        try
        {
            var deposit = _mappingService.MapToDeposit(request);
            var tenantId = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantId))
            {
                return Unauthorized(new { error = "Tenant ID not found in token" });
            }
            
            var result = await _fiscalizationService.FiscalizeDepositAsync(deposit, tenantId);
            
            return result.Success
                ? Ok(new DepositResponse 
                { 
                    Success = true, 
                    FCDC = result.FIC, 
                    Message = "Deposit registered successfully" 
                })
                : BadRequest(new DepositResponse 
                { 
                    Success = false, 
                    Message = "Deposit registration failed",
                    Errors = result.Errors 
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering deposit");
            return StatusCode(500, new DepositResponse
            {
                Success = false,
                Message = "Internal server error"
            });
        }
    }
    
    /// <summary>
    /// Generating test invoice for demo
    /// </summary>
    [HttpGet("demo/generate-invoice")]
    public IActionResult GenerateTestInvoice()
    {
        var invoice = _mappingService.GenerateTestInvoice();
        return Ok(invoice);
    }
    
    /// <summary>
    /// Health checking endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new HealthResponse
        { 
            Status = "healthy", 
            Timestamp = DateTime.UtcNow,
            Environment = "TEST",
            Version = "1.0.0"
        });
    }
    
    /// <summary>
    /// Getting configuration info
    /// </summary>
    [HttpGet("config")]
    public IActionResult GetConfig([FromServices] IConfiguration configuration)
    {
        return Ok(new ConfigResponse
        {
            TIN = configuration["Fiscalization:TIN"],
            BusinessUnitCode = configuration["Fiscalization:BusinessUnitCode"],
            TCRCode = configuration["Fiscalization:TCRCode"],
            SoftwareCode = configuration["Fiscalization:SoftwareCode"],
            Environment = configuration["Environment"] ?? "TEST",
            Message = "Current configuration"
        });
    }
}