using MontenegroFiscalization.Api.DTOs;
using MontenegroFiscalization.Core.Models;

namespace MontenegroFiscalization.Api.Services;

public interface IMappingService
{
    Invoice MapToInvoice(InvoiceRequest request);
    Deposit MapToDeposit(DepositRequest request);
    string GenerateQRCodeUrl(string? iic, string? fic);
    InvoiceRequest GenerateTestInvoice();
    string FormatInvoiceNumber(Invoice invoice);
}