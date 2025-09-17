namespace MontenegroFiscalization.Api.DTOs;

public class DepositResponse
{
    public bool Success { get; set; }
    public string? FCDC { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}

public class HealthResponse
{
    public string Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string Environment { get; set; }
    public string Version { get; set; }
}

public class ConfigResponse
{
    public string TIN { get; set; }
    public string BusinessUnitCode { get; set; }
    public string TCRCode { get; set; }
    public string SoftwareCode { get; set; }
    public string Environment { get; set; }
    public string Message { get; set; }
}