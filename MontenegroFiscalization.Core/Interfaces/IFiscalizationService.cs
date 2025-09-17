namespace MontenegroFiscalization.Core.Interfaces;

using MontenegroFiscalization.Core.Models;

public interface IFiscalizationService
{
    Task<FiscalizationResult> FiscalizeInvoiceAsync(Invoice invoice, string tenantId);
    Task<FiscalizationResult> FiscalizeDepositAsync(Deposit deposit, string tenantId);
}

public class FiscalizationResult
{
    public bool Success { get; set; }
    public string? FIC { get; set; }
    public string? UUID { get; set; }
    public List<string> Errors { get; set; } = new();
}