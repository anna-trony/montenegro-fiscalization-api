using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MontenegroFiscalization.Core.Services;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;

namespace MontenegroFiscalization.Infrastructure.Services;

public class VaultCertificateService : ICertificateService
{
    private readonly IVaultClient? _vaultClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<VaultCertificateService> _logger;
    private readonly bool _useVault;
    
    public VaultCertificateService(
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<VaultCertificateService> logger)
    {
        _cache = cache;
        _logger = logger;
        
        // Checking if Vault is configured
        var vaultUrl = configuration["Vault:Url"];
        var vaultToken = configuration["Vault:Token"];
        
        if (!string.IsNullOrEmpty(vaultUrl) && !string.IsNullOrEmpty(vaultToken))
        {
            try
            {
                var authMethod = new TokenAuthMethodInfo(vaultToken);
                var vaultClientSettings = new VaultClientSettings(vaultUrl, authMethod);
                _vaultClient = new VaultClient(vaultClientSettings);
                _useVault = true;
                _logger.LogInformation("Vault client initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Vault client, will use local storage");
                _useVault = false;
            }
        }
        else
        {
            _logger.LogWarning("Vault not configured, using local certificate storage");
            _useVault = false;
        }
    }
    
    public async Task<X509Certificate2?> GetCertificateAsync(string tenantId)
    {
        // Checking cache first
        var cacheKey = $"cert_{tenantId}";
        if (_cache.TryGetValue<X509Certificate2>(cacheKey, out var cachedCert))
        {
            _logger.LogDebug("Certificate loaded from cache for tenant {TenantId}", tenantId);
            return cachedCert;
        }
        
        try
        {
            X509Certificate2? certificate = null;
            
            if (_useVault && _vaultClient != null)
            {
                // Loading from Vault
                var secretPath = $"fiscalization/certificates/{tenantId}";
                var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                    path: secretPath,
                    mountPoint: "secret"
                );
                
                if (secret.Data.Data.TryGetValue("certificate", out var certData) &&
                    secret.Data.Data.TryGetValue("password", out var password))
                {
                    var certBytes = Convert.FromBase64String(certData.ToString()!);
                    certificate = new X509Certificate2(
                        certBytes,
                        password.ToString(),
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet
                    );
                }
            }
            else
            {
                // Loading from local file (for testing)
                var certPath = $"Certificates/{tenantId}.pfx";
                if (File.Exists(certPath))
                {
                    certificate = new X509Certificate2(
                        certPath,
                        "password", // TODO: Get from secure config
                        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet
                    );
                }
            }
            
            if (certificate != null)
            {
                // Caching for 5 minutes
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                
                _cache.Set(cacheKey, certificate, cacheOptions);
                _logger.LogInformation("Certificate loaded for tenant {TenantId}", tenantId);
            }
            
            return certificate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load certificate for tenant {TenantId}", tenantId);
            return null;
        }
    }
    
    public async Task<bool> StoreCertificateAsync(string tenantId, byte[] certificateData, string password)
    {
        try
        {
            // Validating certificate first
            var cert = new X509Certificate2(certificateData, password, X509KeyStorageFlags.EphemeralKeySet);
            
            if (cert.NotAfter < DateTime.UtcNow)
            {
                _logger.LogWarning("Cannot store expired certificate for tenant {TenantId}", tenantId);
                return false;
            }
            
            if (_useVault && _vaultClient != null)
            {
                // Storing in Vault
                var secretPath = $"fiscalization/certificates/{tenantId}";
                var secretData = new Dictionary<string, object>
                {
                    ["certificate"] = Convert.ToBase64String(certificateData),
                    ["password"] = password,
                    ["thumbprint"] = cert.Thumbprint,
                    ["subject"] = cert.Subject,
                    ["issuer"] = cert.Issuer,
                    ["notBefore"] = cert.NotBefore.ToString("O"),
                    ["notAfter"] = cert.NotAfter.ToString("O"),
                    ["uploadedAt"] = DateTime.UtcNow.ToString("O")
                };
                
                await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
                    path: secretPath,
                    data: secretData,
                    mountPoint: "secret"
                );
            }
            else
            {
                // Storing locally (for testing)
                var certDir = "Certificates";
                if (!Directory.Exists(certDir))
                    Directory.CreateDirectory(certDir);
                    
                var certPath = Path.Combine(certDir, $"{tenantId}.pfx");
                await File.WriteAllBytesAsync(certPath, certificateData);
                
                // Storing metadata
                var metaPath = Path.Combine(certDir, $"{tenantId}.json");
                var metadata = new
                {
                    thumbprint = cert.Thumbprint,
                    subject = cert.Subject,
                    issuer = cert.Issuer,
                    notBefore = cert.NotBefore,
                    notAfter = cert.NotAfter,
                    uploadedAt = DateTime.UtcNow
                };
                await File.WriteAllTextAsync(metaPath, System.Text.Json.JsonSerializer.Serialize(metadata));
            }
            
            // Clearing cache
            _cache.Remove($"cert_{tenantId}");
            
            _logger.LogInformation("Certificate stored successfully for tenant {TenantId}", tenantId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store certificate for tenant {TenantId}", tenantId);
            return false;
        }
    }
    
    public async Task<bool> ValidateCertificateAsync(string tenantId)
    {
        var cert = await GetCertificateAsync(tenantId);
        if (cert == null) return false;
        
        // Checking expiration
        if (cert.NotAfter < DateTime.UtcNow)
        {
            _logger.LogWarning("Certificate expired for tenant {TenantId}", tenantId);
            return false;
        }
        
        // Warning if expires soon
        var daysUntilExpiry = (cert.NotAfter - DateTime.UtcNow).Days;
        if (daysUntilExpiry < 30)
        {
            _logger.LogWarning("Certificate expires in {Days} days for tenant {TenantId}", 
                daysUntilExpiry, tenantId);
        }
        
        return true;
    }
    
    public async Task<CertificateInfo?> GetCertificateInfoAsync(string tenantId)
    {
        var cert = await GetCertificateAsync(tenantId);
        if (cert == null) return null;
        
        var daysUntilExpiry = (cert.NotAfter - DateTime.UtcNow).Days;
        
        return new CertificateInfo
        {
            Subject = cert.Subject,
            Issuer = cert.Issuer,
            Thumbprint = cert.Thumbprint,
            NotBefore = cert.NotBefore,
            NotAfter = cert.NotAfter,
            IsValid = cert.NotAfter > DateTime.UtcNow,
            DaysUntilExpiry = daysUntilExpiry
        };
    }
}