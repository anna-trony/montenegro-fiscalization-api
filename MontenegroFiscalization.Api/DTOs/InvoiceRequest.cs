using MontenegroFiscalization.Core.Models;

namespace MontenegroFiscalization.Api.DTOs;

public class InvoiceRequest
{
    public long Number { get; set; }
    public DateTime? IssueDateTime { get; set; }
    public string? BusinessUnit { get; set; }
    public string? TCRCode { get; set; }
    public decimal TotalWithoutVAT { get; set; }
    public decimal TotalVAT { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsCash { get; set; }
    public InvoiceType? InvoiceType { get; set; }
    public string? OperatorCode { get; set; }
    public string? OperatorName { get; set; }
    public List<InvoiceItemDto>? Items { get; set; }
    public List<PaymentMethodDto>? PaymentMethods { get; set; }
    public BuyerDto? Buyer { get; set; }
}

public class InvoiceItemDto
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal PriceBeforeVAT { get; set; }
    public decimal VATRate { get; set; }
    public decimal? VATAmount { get; set; }
    public decimal? TotalPrice { get; set; }
}

public class PaymentMethodDto
{
    public string Type { get; set; } = "CASH";
    public decimal Amount { get; set; }
}

public class BuyerDto
{
    public string IdType { get; set; } = "TIN";
    public string IdNumber { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Town { get; set; }
    public string? Country { get; set; }
}