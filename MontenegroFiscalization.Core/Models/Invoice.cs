// Core/Models/Invoice.cs
namespace MontenegroFiscalization.Core.Models;

public class Invoice
{
    // Basic Info
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public long Number { get; set; }
    public DateTime IssueDateTime { get; set; }
    public string BusinessUnit { get; set; }
    public string TCRCode { get; set; }
    
    // amounts
    public decimal TotalPriceWithoutVAT { get; set; }
    public decimal TotalVAT { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal TotalPriceToPay { get; set; }
    
    // Fiscalization
    public string? IIC { get; set; }
    public string? IICSignature { get; set; }
    public string? FIC { get; set; }
    public string? QRCode { get; set; }
    
    // Type
    public bool IsCash { get; set; }
    public InvoiceType Type { get; set; }
    public bool IsReverseCharge { get; set; }
    public bool IsSimplifiedInvoice { get; set; }
    
    // Subsequent Delivery
    public bool IsSubsequentDelivery { get; set; }
    public SubsequentDeliveryType? SubsequentDeliveryType { get; set; }
    
    // Relations
    public List<InvoiceItem> Items { get; set; } = new();
    public List<PaymentMethod> PaymentMethods { get; set; } = new();
    public List<VATSummary> VATSummaries { get; set; } = new();
    public Operator? Operator { get; set; }
    public Buyer? Buyer { get; set; }
    
    // Corrective Invoice
    public CorrectiveInvoice? Corrective { get; set; }
    
    // Summary Invoice References
    public List<SummaryInvoiceReference>? SummaryReferences { get; set; }
    
    // Period for Periodical invoices
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    
    public string? ParagonBlockNumber { get; set; }
    
    public bool IsExport { get; set; }
    public decimal? ExportAmount { get; set; }
    
    public decimal? TaxFreeAmount { get; set; }
        public bool IsBadDebt { get; set; }
    
    public DateTime? SupplyDate { get; set; }
    
    public string? Currency { get; set; }
    public decimal? ExchangeRate { get; set; }
    
    public DateTime? PaymentDeadline { get; set; }
}

public enum InvoiceType
{
    Normal,
    Corrective,
    Summary,
    Periodical,
    Advance,
    CreditNote
}

public enum SubsequentDeliveryType
{
    NoInternet,
    BoundBook,
    Service,
    TechnicalError,
    BusinessNeed
}

public class InvoiceItem
{
    public string Name { get; set; }
    public string Code { get; set; }
    public string Unit { get; set; }
    public decimal Quantity { get; set; }
    public decimal PriceBeforeVAT { get; set; }
    public decimal PriceAfterVAT { get; set; }
    public decimal VATRate { get; set; }
    public decimal VATAmount { get; set; }
    public decimal TotalPriceBeforeVAT { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal? Rebate { get; set; }
    public bool RebateReducingBaseAmount { get; set; }
    public string? ExemptionCode { get; set; }
    public bool IsInvestment { get; set; }
    public VoucherInfo? Voucher { get; set; }
}

public class VoucherInfo
{
    public string SerialNumber { get; set; }
    public decimal Value { get; set; }
}

public class PaymentMethod
{
    public string Type { get; set; } // CASH, CARD, ACCOUNT, ORDER, ADVANCE, FACTORING, OTHER
    public decimal Amount { get; set; }
    public string? AdvanceIIC { get; set; } // For advance payments
    public string? CompanyCard { get; set; } // For company cards
    public CardPaymentInfo? CardInfo { get; set; }
}

public class CardPaymentInfo
{
    public string CardNumber { get; set; } // Last 4 digits
    public string AuthorizationCode { get; set; }
    public string TerminalId { get; set; }
    public DateTime TransactionDateTime { get; set; }
}

public class Operator
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string? User { get; set; }
}

public class Buyer
{
    public string IdType { get; set; } // TIN, ID, PASS, VAT, TAX, SOC
    public string IdNumber { get; set; }
    public string Name { get; set; }
    public string? Address { get; set; }
    public string? Town { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? PostalCode { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsDiplomat { get; set; }
    public string? DiplomatId { get; set; } // TIC - Tax Identification Card
}

public class VATSummary
{
    public int NumberOfItems { get; set; }
    public decimal PriceBeforeVAT { get; set; }
    public decimal VATRate { get; set; }
    public decimal VATAmount { get; set; }
    public string? ExemptionCode { get; set; }
}

public class CorrectiveInvoice
{
    public string IIC { get; set; }
    public DateTime IssueDateTime { get; set; }
    public string Type { get; set; } // CORRECTIVE or ERROR_CORRECTIVE
    public decimal OriginalAmount { get; set; }
}

public class SummaryInvoiceReference
{
    public string IIC { get; set; }
    public DateTime IssueDateTime { get; set; }
    public decimal Amount { get; set; }
}

// VAT Exemption Codes
public static class VATExemptionCodes
{
    public const string VAT_CL17 = "VAT_CL17"; // Place of supply of services
    public const string VAT_CL20 = "VAT_CL20"; // Tax base and tax base correction
    public const string VAT_CL26 = "VAT_CL26"; // Public interest exemptions
    public const string VAT_CL27 = "VAT_CL27"; // Other exemptions
    public const string VAT_CL28 = "VAT_CL28"; // Import exemptions
    public const string VAT_CL29 = "VAT_CL29"; // Temporary import exemptions
    public const string VAT_CL30 = "VAT_CL30"; // Special exemptions (Diplomats)
    public const string VAT_CL44 = "VAT_CL44"; // Special taxation procedures
}