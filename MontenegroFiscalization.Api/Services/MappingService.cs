using MontenegroFiscalization.Api.DTOs;
using MontenegroFiscalization.Core.Models;

namespace MontenegroFiscalization.Api.Services;

public class MappingService : IMappingService
{
    private readonly IConfiguration _configuration;
    
    public MappingService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public Invoice MapToInvoice(InvoiceRequest request)
    {
        ValidateAndFixItems(request.Items);
        
        return new Invoice
        {
            Number = request.Number,
            IssueDateTime = request.IssueDateTime ?? DateTime.Now,
            BusinessUnit = request.BusinessUnit ?? _configuration["Fiscalization:BusinessUnitCode"],
            TCRCode = request.TCRCode ?? _configuration["Fiscalization:TCRCode"],
            TotalPriceWithoutVAT = request.TotalWithoutVAT,
            TotalVAT = request.TotalVAT,
            TotalPrice = request.TotalPrice,
            IsCash = request.IsCash,
            Type = request.InvoiceType ?? InvoiceType.Normal,
            Items = MapItems(request.Items),
            PaymentMethods = MapPaymentMethods(request.PaymentMethods),
            Buyer = MapBuyer(request.Buyer),
            Operator = new Operator 
            { 
                Code = request.OperatorCode ?? "op123",
                Name = request.OperatorName ?? "Default Operator"
            }
        };
    }
    
    public Deposit MapToDeposit(DepositRequest request)
    {
        return new Deposit
        {
            ChangeDateTime = DateTime.Now,
            Operation = request.Operation,
            Amount = request.Amount,
            TCRCode = request.TCRCode ?? _configuration["Fiscalization:TCRCode"],
            BusinessUnit = request.BusinessUnit ?? _configuration["Fiscalization:BusinessUnitCode"],
            Operator = new Operator 
            { 
                Code = request.OperatorCode ?? "op123",
                Name = request.OperatorName ?? "Default Operator"
            }
        };
    }
    
    public string GenerateQRCodeUrl(string? iic, string? fic)
    {
        var tin = _configuration["Fiscalization:TIN"];
        var verifyUrl = _configuration["TaxAdministration:Test:VerifyUrl"];
        return $"{verifyUrl}?iic={iic}&tin={tin}&fic={fic}";
    }
    
    private void ValidateAndFixItems(List<InvoiceItemDto>? items)
    {
        if (items == null) return;
        
        foreach (var item in items)
        {
            if (item.Quantity == 0) 
                item.Quantity = 1;
                
            if (item.TotalPrice == 0 && item.Quantity > 0)
            {
                item.VATAmount ??= item.PriceBeforeVAT * item.Quantity * item.VATRate / 100;
                item.TotalPrice = item.PriceBeforeVAT * item.Quantity + item.VATAmount;
            }
        }
    }
    
    private List<InvoiceItem> MapItems(List<InvoiceItemDto>? items)
    {
        if (items == null) return new List<InvoiceItem>();
        
        return items.Select(i => new InvoiceItem
        {
            Name = i.Name,
            Code = i.Code,
            Unit = i.Unit ?? "PCS",
            Quantity = i.Quantity,
            PriceBeforeVAT = i.PriceBeforeVAT,
            VATRate = i.VATRate,
            VATAmount = i.VATAmount ?? (i.PriceBeforeVAT * i.Quantity * i.VATRate / 100),
            TotalPrice = i.TotalPrice ?? (i.PriceBeforeVAT * i.Quantity * (1 + i.VATRate / 100))
        }).ToList();
    }
    
    private List<PaymentMethod> MapPaymentMethods(List<PaymentMethodDto>? methods)
    {
        if (methods == null) return new List<PaymentMethod>();
        
        return methods.Select(m => new PaymentMethod
        {
            Type = m.Type,
            Amount = m.Amount
        }).ToList();
    }
    
    private Buyer? MapBuyer(BuyerDto? buyer)
    {
        if (buyer == null) return null;
        
        return new Buyer
        {
            IdType = buyer.IdType,
            IdNumber = buyer.IdNumber,
            Name = buyer.Name,
            Address = buyer.Address,
            Town = buyer.Town,
            Country = buyer.Country
        };
    }

    public string FormatInvoiceNumber(Invoice invoice)
    {
        var businessUnit = _configuration["Fiscalization:BusinessUnitCode"];
        var tcrCode = _configuration["Fiscalization:TCRCode"];
        return $"{businessUnit}/{invoice.Number}/{invoice.IssueDateTime.Year}/{tcrCode}";
    }

    public InvoiceRequest GenerateTestInvoice()
    {
        var random = new Random();
        var invoice = new InvoiceRequest
        {
            Number = random.Next(1000, 9999),
            IssueDateTime = DateTime.Now,
            IsCash = true,
            Items = new List<InvoiceItemDto>
            {
                new() { Name = "Product A", Code = "001", Unit = "PCS", Quantity = 2, PriceBeforeVAT = 50, VATRate = 21 },
                new() { Name = "Product B", Code = "002", Unit = "PCS", Quantity = 1, PriceBeforeVAT = 30, VATRate = 21 }
            },
            PaymentMethods = new List<PaymentMethodDto>
            {
                new() { Type = "CASH", Amount = 157.60m }
            }
        };
        
        invoice.TotalWithoutVAT = invoice.Items.Sum(i => i.PriceBeforeVAT * i.Quantity);
        invoice.TotalVAT = invoice.TotalWithoutVAT * 0.21m;
        invoice.TotalPrice = invoice.TotalWithoutVAT + invoice.TotalVAT;
        
        foreach (var item in invoice.Items)
        {
            item.VATAmount = item.PriceBeforeVAT * item.Quantity * item.VATRate / 100;
            item.TotalPrice = item.PriceBeforeVAT * item.Quantity + item.VATAmount;
        }
        
        return invoice;
    }
}