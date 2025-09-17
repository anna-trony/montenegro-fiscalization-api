// Infrastructure/Services/FiscalizationService.cs
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Net.Http;
using MontenegroFiscalization.Core.Interfaces;
using MontenegroFiscalization.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MontenegroFiscalization.Infrastructure.Services;

public class FiscalizationService : IFiscalizationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FiscalizationService> _logger;
    private readonly HttpClient _httpClient;
    
    // configuration
    private readonly string _tin;
    private readonly string _businessUnitCode;
    private readonly string _tcrCode;
    private readonly string _softwareCode;
    private readonly string _maintainerCode;
    private readonly bool _isIssuerInVAT;
    private readonly string _fiscalizationUrl;
    private readonly string _verifyUrl;
    private readonly string _namespace;
    private readonly string _sellerName;
    private readonly string _sellerAddress;
    private readonly string _sellerTown;
    private readonly string _sellerCountry;
    private readonly bool _demoMode;
    
    // certificates
    private X509Certificate2? _signatureCertificate;
    private X509Certificate2? _sealCertificate;
    
    // error codes dictionary
    private readonly Dictionary<int, string> ErrorMessages = new()
    {
        {0, "Exception during XML extraction for size check"},
        {1, "XML message exceeds allowed size"},
        {2, "Client time differs from server time by more than allowed (6 hours) or is in the future"},
        {10, "Exception during XML extraction for XSD validation"},
        {11, "XML validation failed"},
        {20, "Exception during signature extraction"},
        {21, "Signature element missing in XML message"},
        {22, "XML request element missing"},
        {23, "Exception during signature element extraction"},
        {24, "More than one signature element provided"},
        {25, "Wrong XML element signed"},
        {26, "Wrong signature method specified"},
        {27, "Wrong canonicalization method specified"},
        {28, "Wrong digest method specified"},
        {29, "Cryptographic signature incorrect"},
        {30, "Digest calculation incorrect"},
        {31, "Overall signature incorrect"},
        {32, "More key information than required"},
        {33, "Certificate is not X509 type"},
        {34, "Certificate not valid"},
        {35, "Certificate not issued by registered CA"},
        {36, "Certificate expired"},
        {37, "TIN mismatch between XML and certificate"},
        {38, "Certificate revoked"},
        {39, "Certificate status unknown"},
        {40, "Invoice amount too large for cash invoice"},
        {41, "Business unit code does not refer to active unit"},
        {42, "Software code does not refer to active software"},
        {43, "Maintainer code does not refer to active maintainer"},
        {44, "Issuer VAT status does not match IsIssuerInVAT attribute"},
        {45, "ValidFrom cannot be in the past"},
        {46, "ValidTo cannot be in the past"},
        {47, "ValidTo cannot be before ValidFrom"},
        {48, "Active TCR cannot be updated"},
        {49, "Change date time differs from CIS time by more than allowed"},
        {50, "Cash amount for INITIAL operation cannot be negative"},
        {51, "Cash amount cannot be zero for WITHDRAW operation"},
        {52, "Taxpayer does not exist in taxpayer registry"},
        {53, "TCR code does not refer to registered or active TCR"},
        {54, "ID type must be TIN"},
        {55, "Taxpayer not active in registry"},
        {56, "Cash deposit with INITIAL operation already registered for current day"},
        {57, "Deactivated TCR cannot be changed"},
        {58, "Initial cash deposit must be registered before invoice fiscalization"}
    };
    
    public FiscalizationService(
        IConfiguration configuration,
        ILogger<FiscalizationService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        
        // Loading configuration
        _tin = configuration["Fiscalization:TIN"] ?? "02004003";
        _businessUnitCode = configuration["Fiscalization:BusinessUnitCode"] ?? "bb123bb123";
        _tcrCode = configuration["Fiscalization:TCRCode"] ?? "cc123cc123";
        _softwareCode = configuration["Fiscalization:SoftwareCode"] ?? "ss123ss123";
        _maintainerCode = configuration["Fiscalization:MaintainerCode"] ?? "mm123mm123";
        _isIssuerInVAT = bool.Parse(configuration["Fiscalization:IsInVAT"] ?? "true");
        _sellerName = configuration["Fiscalization:SellerName"] ?? "Test Company";
        _sellerAddress = configuration["Fiscalization:SellerAddress"] ?? "Test Address 1";
        _sellerTown = configuration["Fiscalization:SellerTown"] ?? "Podgorica";
        _sellerCountry = configuration["Fiscalization:SellerCountry"] ?? "MNE";
        _demoMode = bool.Parse(configuration["Fiscalization:DemoMode"] ?? "true");
        
        // Getting environment
        var environment = configuration["Environment"] ?? "Test";
        _fiscalizationUrl = configuration[$"TaxAdministration:{environment}:ServiceUrl"] ?? "https://efi-test.tax.gov.me/fs-v1";
        _verifyUrl = configuration[$"TaxAdministration:{environment}:VerifyUrl"] ?? "https://efi.tax.gov.me/fs-v1/verify";
        _namespace = configuration["TaxAdministration:Namespace"] ?? "https://efi.tax.gov.me/fs/schema";
        
        _logger.LogInformation("Using {Environment} environment: {Url}", environment, _fiscalizationUrl);
        _logger.LogInformation("Demo Mode: {DemoMode}", _demoMode);
        
        if (!_demoMode)
        {
            LoadCertificates();
        }
        else
        {
            _logger.LogWarning("Running in DEMO MODE - No certificates loaded, using mock data");
        }
    }
    
    private void LoadCertificates()
    {
        try
        {
            var signaturePath = _configuration["Certificates:Signature:Path"];
            var signaturePassword = _configuration["Certificates:Signature:Password"];
            
            if (!string.IsNullOrEmpty(signaturePath) && File.Exists(signaturePath))
            {
                _signatureCertificate = new X509Certificate2(
                    signaturePath,
                    signaturePassword,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet
                );
                _logger.LogInformation("Signature certificate loaded: {Subject}", _signatureCertificate.Subject);
            }
            
            var sealPath = _configuration["Certificates:Seal:Path"];
            var sealPassword = _configuration["Certificates:Seal:Password"];
            
            if (!string.IsNullOrEmpty(sealPath) && File.Exists(sealPath))
            {
                _sealCertificate = new X509Certificate2(
                    sealPath,
                    sealPassword,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet
                );
                _logger.LogInformation("Seal certificate loaded: {Subject}", _sealCertificate.Subject);
            }
            
            CheckCertificateExpiration(_signatureCertificate, "Signature");
            CheckCertificateExpiration(_sealCertificate, "Seal");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load certificates");
        }
    }
    
    private void CheckCertificateExpiration(X509Certificate2? cert, string name)
    {
        if (cert == null) return;
        
        if (cert.NotAfter < DateTime.Now)
        {
            _logger.LogError("{Name} certificate has expired on {Date}", name, cert.NotAfter);
        }
        else if (cert.NotAfter < DateTime.Now.AddDays(30))
        {
            _logger.LogWarning("{Name} certificate expires soon on {Date}", name, cert.NotAfter);
        }
    }
    
    public async Task<FiscalizationResult> FiscalizeInvoiceAsync(Invoice invoice, string tenantId)
    {
        try
        {
            var iic = GenerateIIC(invoice);
            invoice.IIC = iic.Code;
            invoice.IICSignature = iic.Signature;
            
            _logger.LogInformation("Generated IIC: {IIC} for invoice {Number}", iic.Code, invoice.Number);
            
            var xmlRequest = BuildInvoiceXml(invoice);
            
            if (_demoMode)
            {
                _logger.LogInformation("DEMO MODE: Returning mock FIC for invoice {Number}", invoice.Number);
                return new FiscalizationResult
                {
                    Success = true,
                    FIC = $"DEMO-FIC-{Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper()}",
                    UUID = Guid.NewGuid().ToString()
                };
            }
            
            var signedXml = SignXml(xmlRequest);
            var soapRequest = WrapInSoapEnvelope(signedXml);
            
            var response = await SendToTaxAdmin(soapRequest);
            var result = ParseInvoiceResponse(response);
            
            if (result.Success)
            {
                _logger.LogInformation("Invoice {Number} fiscalized successfully with FIC: {FIC}", 
                    invoice.Number, result.FIC);
            }
            else
            {
                _logger.LogError("Fiscalization failed: {Errors}", string.Join(", ", result.Errors));
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fiscalization failed for invoice {Number}", invoice.Number);
            return new FiscalizationResult
            {
                Success = false,
                Errors = new List<string> { ex.Message }
            };
        }
    }
    
    public async Task<FiscalizationResult> FiscalizeDepositAsync(Deposit deposit, string tenantId)
    {
        try
        {
            _logger.LogInformation("Fiscalizing deposit: {Operation} {Amount}", deposit.Operation, deposit.Amount);
            
            if (_demoMode)
            {
                _logger.LogInformation("DEMO MODE: Returning mock FCDC");
                return new FiscalizationResult
                {
                    Success = true,
                    FIC = $"DEMO-FCDC-{Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper()}",
                    UUID = Guid.NewGuid().ToString()
                };
            }
            
            var xmlRequest = BuildDepositXml(deposit);
            var signedXml = SignXml(xmlRequest);
            var soapRequest = WrapInSoapEnvelope(signedXml);
            
            var response = await SendToTaxAdmin(soapRequest);
            var result = ParseDepositResponse(response);
            
            if (result.Success)
            {
                _logger.LogInformation("Deposit fiscalized successfully with FCDC: {FCDC}", result.FIC);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deposit fiscalization failed");
            return new FiscalizationResult
            {
                Success = false,
                Errors = new List<string> { ex.Message }
            };
        }
    }
    
    private (string Code, string Signature) GenerateIIC(Invoice invoice)
    {
        if (_demoMode || _signatureCertificate == null)
        {
            _logger.LogWarning("DEMO MODE: Generating mock IIC without certificate");
            
            // Generating demo IIC based on invoice data
            var iicInput = $"{_tin}|{invoice.IssueDateTime:yyyy-MM-ddTHH:mm:sszzz}|{invoice.Number}|" +
                          $"{_businessUnitCode}|{_tcrCode}|{_softwareCode}|{invoice.TotalPrice:0.00}";
            
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(iicInput));
                var iicCode = BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 32);
                var signature = Convert.ToBase64String(hashBytes);
                
                return (iicCode, signature);
            }
        }
        
        var iicInputReal = $"{_tin}|{invoice.IssueDateTime:yyyy-MM-ddTHH:mm:sszzz}|{invoice.Number}|" +
                           $"{_businessUnitCode}|{_tcrCode}|{_softwareCode}|{invoice.TotalPrice:0.00}";
        
        _logger.LogDebug("IIC Input: {Input}", iicInputReal);
        
        using (var rsa = _signatureCertificate.GetRSAPrivateKey())
        {
            if (rsa == null)
            {
                throw new InvalidOperationException("Certificate does not contain private key");
            }
            
            byte[] inputBytes = Encoding.UTF8.GetBytes(iicInputReal);
            byte[] signatureBytes = rsa.SignData(inputBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            
            string signatureHex = BitConverter.ToString(signatureBytes).Replace("-", "");
            
            using (var md5 = MD5.Create())
            {
                byte[] iicBytes = md5.ComputeHash(signatureBytes);
                string iicCode = BitConverter.ToString(iicBytes).Replace("-", "");
                
                return (iicCode, signatureHex);
            }
        }
    }
        
    private string BuildInvoiceXml(Invoice invoice)
    {
        var uuid = Guid.NewGuid().ToString();
        var sendDateTime = DateTime.Now;
        
        var xml = new XmlDocument();
        var request = xml.CreateElement("RegisterInvoiceRequest", _namespace);
        request.SetAttribute("Id", "Request");
        request.SetAttribute("Version", "1");
        xml.AppendChild(request);
        
        // Header
        var header = xml.CreateElement("Header", _namespace);
        header.SetAttribute("UUID", uuid);
        header.SetAttribute("SendDateTime", sendDateTime.ToString("yyyy-MM-ddTHH:mm:sszzz"));
        request.AppendChild(header);
        
        // Invoice
        var invoiceElement = xml.CreateElement("Invoice", _namespace);
        invoiceElement.SetAttribute("InvType", GetInvoiceType(invoice));
        invoiceElement.SetAttribute("TypeOfInv", invoice.IsCash ? "CASH" : "NONCASH");
        invoiceElement.SetAttribute("IssueDateTime", invoice.IssueDateTime.ToString("yyyy-MM-ddTHH:mm:sszzz"));
        invoiceElement.SetAttribute("InvNum", $"{_businessUnitCode}/{invoice.Number}/{invoice.IssueDateTime.Year}/{_tcrCode}");
        invoiceElement.SetAttribute("InvOrdNum", invoice.Number.ToString());
        invoiceElement.SetAttribute("TCRCode", _tcrCode);
        invoiceElement.SetAttribute("IsIssuerInVAT", _isIssuerInVAT.ToString().ToLower());
        
        if (!_isIssuerInVAT)
        {
            invoiceElement.SetAttribute("TaxFreeAmt", invoice.TotalPriceWithoutVAT.ToString("0.00"));
        }
        
        invoiceElement.SetAttribute("TotPriceWoVAT", invoice.TotalPriceWithoutVAT.ToString("0.00"));
        
        if (_isIssuerInVAT)
        {
            invoiceElement.SetAttribute("TotVATAmt", invoice.TotalVAT.ToString("0.00"));
        }
        
        invoiceElement.SetAttribute("TotPrice", invoice.TotalPrice.ToString("0.00"));
        invoiceElement.SetAttribute("OperatorCode", invoice.Operator?.Code ?? "op123");
        invoiceElement.SetAttribute("BusinUnitCode", _businessUnitCode);
        invoiceElement.SetAttribute("SoftCode", _softwareCode);
        invoiceElement.SetAttribute("IsReverseCharge", "false");
        invoiceElement.SetAttribute("IIC", invoice.IIC ?? "");
        invoiceElement.SetAttribute("IICSignature", invoice.IICSignature ?? "");
        
        // Payment methods
        if (invoice.PaymentMethods?.Count > 0)
        {
            var payMethods = xml.CreateElement("PayMethods", _namespace);
            foreach (var pm in invoice.PaymentMethods)
            {
                var payMethod = xml.CreateElement("PayMethod", _namespace);
                payMethod.SetAttribute("Type", pm.Type);
                payMethod.SetAttribute("Amt", pm.Amount.ToString("0.00"));
                payMethods.AppendChild(payMethod);
            }
            invoiceElement.AppendChild(payMethods);
        }
        
        // Seller
        var seller = xml.CreateElement("Seller", _namespace);
        seller.SetAttribute("IDType", "TIN");
        seller.SetAttribute("IDNum", _tin);
        seller.SetAttribute("Name", XmlEscape(_sellerName));
        seller.SetAttribute("Address", XmlEscape(_sellerAddress));
        seller.SetAttribute("Town", XmlEscape(_sellerTown));
        seller.SetAttribute("Country", _sellerCountry);
        invoiceElement.AppendChild(seller);
        
        // Items
        if (invoice.Items?.Count > 0)
        {
            var items = xml.CreateElement("Items", _namespace);
            foreach (var item in invoice.Items)
            {
                var itemElement = xml.CreateElement("I", _namespace);
                itemElement.SetAttribute("N", XmlEscape(item.Name.Length > 40 ? item.Name.Substring(0, 40) : item.Name));
                itemElement.SetAttribute("C", item.Code);
                itemElement.SetAttribute("U", item.Unit);
                itemElement.SetAttribute("Q", item.Quantity.ToString("0.000"));
                itemElement.SetAttribute("UPB", item.PriceBeforeVAT.ToString("0.0000"));
                itemElement.SetAttribute("UPA", (item.PriceBeforeVAT * (1 + item.VATRate / 100)).ToString("0.0000"));
                itemElement.SetAttribute("R", "0.0000");
                itemElement.SetAttribute("RR", "false");
                itemElement.SetAttribute("PB", (item.PriceBeforeVAT * item.Quantity).ToString("0.0000"));
                if (_isIssuerInVAT)
                {
                    itemElement.SetAttribute("VR", item.VATRate.ToString("0.0000"));
                    itemElement.SetAttribute("VA", item.VATAmount.ToString("0.0000"));
                }
                itemElement.SetAttribute("PA", item.TotalPrice.ToString("0.0000"));
                items.AppendChild(itemElement);
            }
            invoiceElement.AppendChild(items);
        }
        
        // VAT summary
        if (_isIssuerInVAT && invoice.Items?.Count > 0)
        {
            var sameTaxes = xml.CreateElement("SameTaxes", _namespace);
            var vatGroups = invoice.Items.GroupBy(i => i.VATRate);
            
            foreach (var group in vatGroups)
            {
                var sameTax = xml.CreateElement("SameTax", _namespace);
                sameTax.SetAttribute("NumOfItems", group.Count().ToString());
                sameTax.SetAttribute("PriceBefVAT", group.Sum(i => i.PriceBeforeVAT * i.Quantity).ToString("0.00"));
                sameTax.SetAttribute("VATRate", group.Key.ToString("0.00"));
                sameTax.SetAttribute("VATAmt", group.Sum(i => i.VATAmount).ToString("0.00"));
                sameTaxes.AppendChild(sameTax);
            }
            invoiceElement.AppendChild(sameTaxes);
        }
        
        request.AppendChild(invoiceElement);
        
        return xml.OuterXml;
    }
    
    private string BuildDepositXml(Deposit deposit)
    {
        var uuid = Guid.NewGuid().ToString();
        var sendDateTime = DateTime.Now;
        
        var xml = new XmlDocument();
        var request = xml.CreateElement("RegisterCashDepositRequest", _namespace);
        request.SetAttribute("Id", "Request");
        request.SetAttribute("Version", "1");
        xml.AppendChild(request);
        
        // Header
        var header = xml.CreateElement("Header", _namespace);
        header.SetAttribute("UUID", uuid);
        header.SetAttribute("SendDateTime", sendDateTime.ToString("yyyy-MM-ddTHH:mm:sszzz"));
        request.AppendChild(header);
        
        // Cash Deposit
        var depositElement = xml.CreateElement("CashDeposit", _namespace);
        depositElement.SetAttribute("ChangeDateTime", deposit.ChangeDateTime.ToString("yyyy-MM-ddTHH:mm:sszzz"));
        depositElement.SetAttribute("Operation", deposit.Operation);
        depositElement.SetAttribute("CashAmt", Math.Abs(deposit.Amount).ToString("0.00"));
        depositElement.SetAttribute("TCRCode", deposit.TCRCode ?? _tcrCode);
        depositElement.SetAttribute("IssuerTIN", _tin);
        request.AppendChild(depositElement);
        
        return xml.OuterXml;
    }
    
    private string GetInvoiceType(Invoice invoice)
    {
        if (invoice.Corrective != null) return "CORRECTIVE";
        if (invoice.Type == InvoiceType.Summary) return "SUMMARY";
        if (invoice.Type == InvoiceType.Advance) return "ADVANCE";
        return "INVOICE";
    }
    
    private string XmlEscape(string? str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return str.Replace("&", "&amp;")
                  .Replace("<", "&lt;")
                  .Replace(">", "&gt;")
                  .Replace("\"", "&quot;")
                  .Replace("'", "&apos;");
    }
    
    private string SignXml(string xmlContent)
    {
        if (_demoMode || _sealCertificate == null)
        {
            _logger.LogWarning("DEMO MODE: Returning unsigned XML");
            return xmlContent;
        }
        
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = true;
            xmlDoc.LoadXml(xmlContent);
            
            var signedXml = new SignedXml(xmlDoc);
            signedXml.SigningKey = _sealCertificate.GetRSAPrivateKey();
            
            var reference = new Reference();
            reference.Uri = "#Request";
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.AddTransform(new XmlDsigExcC14NTransform());
            reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
            
            signedXml.AddReference(reference);
            
            var keyInfo = new KeyInfo();
            var keyInfoData = new KeyInfoX509Data(_sealCertificate);
            keyInfo.AddClause(keyInfoData);
            signedXml.KeyInfo = keyInfo;
            
            signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
            
            signedXml.ComputeSignature();
            
            XmlElement xmlDigitalSignature = signedXml.GetXml();
            xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xmlDigitalSignature, true));
            
            return xmlDoc.OuterXml;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign XML");
            throw;
        }
    }
    
    private string WrapInSoapEnvelope(string signedXml)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/"">
    <SOAP-ENV:Header/>
    <SOAP-ENV:Body>
        {signedXml}
    </SOAP-ENV:Body>
</SOAP-ENV:Envelope>";
    }
    
    private async Task<string> SendToTaxAdmin(string soapXml)
    {
        try
        {
            _logger.LogInformation("Sending request to {Url}", _fiscalizationUrl);
            _logger.LogDebug("Request XML: {Xml}", soapXml);
            
            var content = new StringContent(soapXml, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", "");
            
            var response = await _httpClient.PostAsync(_fiscalizationUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            _logger.LogDebug("Response status: {Status}", response.StatusCode);
            _logger.LogDebug("Response XML: {Xml}", responseContent);
            
            return responseContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send request");
            throw;
        }
    }
    
    private FiscalizationResult ParseInvoiceResponse(string responseXml)
    {
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(responseXml);
            
            var nsmgr = GetNamespaceManager(xmlDoc);
            
            // Checking for fault
            var faultNode = xmlDoc.SelectSingleNode("//soap:Fault", nsmgr);
            if (faultNode != null)
            {
                return ParseFaultResponse(faultNode);
            }
            
            // Extracting FIC
            var ficNode = xmlDoc.SelectSingleNode("//ns:FIC", nsmgr) ?? 
                         xmlDoc.SelectSingleNode("//FIC", nsmgr);
            
            if (ficNode != null)
            {
                var uuidNode = xmlDoc.SelectSingleNode("//ns:Header/@UUID", nsmgr) ??
                              xmlDoc.SelectSingleNode("//Header/@UUID", nsmgr);
                
                return new FiscalizationResult
                {
                    Success = true,
                    FIC = ficNode.InnerText,
                    UUID = uuidNode?.Value ?? Guid.NewGuid().ToString()
                };
            }
            
            return new FiscalizationResult
            {
                Success = false,
                Errors = new List<string> { "FIC not found in response" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse response");
            return new FiscalizationResult
            {
                Success = false,
                Errors = new List<string> { ex.Message }
            };
        }
    }
    
    private FiscalizationResult ParseDepositResponse(string responseXml)
    {
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(responseXml);
            
            var nsmgr = GetNamespaceManager(xmlDoc);
            
            var faultNode = xmlDoc.SelectSingleNode("//soap:Fault", nsmgr);
            if (faultNode != null)
            {
                return ParseFaultResponse(faultNode);
            }
            
            var fcdcNode = xmlDoc.SelectSingleNode("//ns:FCDC", nsmgr) ?? 
                          xmlDoc.SelectSingleNode("//FCDC", nsmgr);
            
            if (fcdcNode != null)
            {
                return new FiscalizationResult
                {
                    Success = true,
                    FIC = fcdcNode.InnerText,
                    UUID = Guid.NewGuid().ToString()
                };
            }
            
            return new FiscalizationResult
            {
                Success = false,
                Errors = new List<string> { "FCDC not found in response" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse deposit response");
            return new FiscalizationResult
            {
                Success = false,
                Errors = new List<string> { ex.Message }
            };
        }
    }
    
    private FiscalizationResult ParseFaultResponse(XmlNode faultNode)
    {
        var faultString = faultNode.SelectSingleNode("faultstring")?.InnerText ?? "Unknown error";
        var errorCodeStr = faultNode.SelectSingleNode("detail/code")?.InnerText;
        
        var errors = new List<string>();
        
        if (int.TryParse(errorCodeStr, out int errorCode) && ErrorMessages.ContainsKey(errorCode))
        {
            errors.Add($"{ErrorMessages[errorCode]} (Code: {errorCode})");
        }
        else
        {
            errors.Add($"{faultString} (Code: {errorCodeStr})");
        }
        
        return new FiscalizationResult
        {
            Success = false,
            Errors = errors
        };
    }
    
    private XmlNamespaceManager GetNamespaceManager(XmlDocument doc)
    {
        var nsmgr = new XmlNamespaceManager(doc.NameTable);
        nsmgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
        nsmgr.AddNamespace("ns", _namespace);
        return nsmgr;
    }
}