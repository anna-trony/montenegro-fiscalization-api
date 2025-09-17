namespace MontenegroFiscalization.Api.DTOs;

public class FiscalizationResponse
{
    public bool Success { get; set; }
    public string? FIC { get; set; }
    public string? UUID { get; set; }
    public string? IIC { get; set; }
    public string? IICSignature { get; set; }
    public string? Message { get; set; }
    public string? QRCodeUrl { get; set; }
    public string? InvoiceNumber { get; set; }
    public List<string>? Errors { get; set; }
}