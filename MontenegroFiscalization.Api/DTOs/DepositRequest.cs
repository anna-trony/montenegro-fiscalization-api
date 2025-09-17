namespace MontenegroFiscalization.Api.DTOs;

public class DepositRequest
{
    public string Operation { get; set; } = "INITIAL"; // INITIAL or WITHDRAW
    public decimal Amount { get; set; }
    public string? TCRCode { get; set; }
    public string? BusinessUnit { get; set; }
    public string? OperatorCode { get; set; }
    public string? OperatorName { get; set; }
}
