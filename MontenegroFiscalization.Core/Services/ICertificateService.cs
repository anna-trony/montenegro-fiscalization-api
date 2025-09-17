using System.Security.Cryptography.X509Certificates;

namespace MontenegroFiscalization.Core.Services;

public interface ICertificateService
{
    Task<X509Certificate2?> GetCertificateAsync(string tenantId);
    Task<bool> StoreCertificateAsync(string tenantId, byte[] certificateData, string password);
    Task<bool> ValidateCertificateAsync(string tenantId);
    Task<CertificateInfo?> GetCertificateInfoAsync(string tenantId);
}

public class CertificateInfo
{
    public string Subject { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Thumbprint { get; set; } = "";
    public DateTime NotBefore { get; set; }
    public DateTime NotAfter { get; set; }
    public bool IsValid { get; set; }
    public int DaysUntilExpiry { get; set; }
}

