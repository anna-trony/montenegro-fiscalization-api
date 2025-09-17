namespace MontenegroFiscalization.Core.Models;

public class FiscalizationAudit
{
    public int Id { get; set; }
    public string TenantId { get; set; } = "";
    public string Type { get; set; } = ""; // INVOICE, DEPOSIT, TCR_REGISTRATION
    public long? InvoiceNumber { get; set; }
    public string? IIC { get; set; }
    public string? FIC { get; set; }
    public string? UUID { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RequestXml { get; set; }
    public string? ResponseXml { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public decimal? Amount { get; set; }
    public string? BusinessUnit { get; set; }
    public string? TCRCode { get; set; }
}

public class TenantConfiguration
{
    public int Id { get; set; }
    public string TenantId { get; set; } = "";
    public string TenantName { get; set; } = "";
    public string TIN { get; set; } = "";
    public string BusinessUnitCode { get; set; } = "";
    public string TCRCode { get; set; } = "";
    public string SoftwareCode { get; set; } = "";
    public string MaintainerCode { get; set; } = "";
    public bool IsInVAT { get; set; }
    public bool IsActive { get; set; }
    public string? CertificateVaultPath { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ApiKey
{
    public int Id { get; set; }
    public string TenantId { get; set; } = "";
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}