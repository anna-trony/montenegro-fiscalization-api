namespace MontenegroFiscalization.Core.Models;

public class Deposit
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime ChangeDateTime { get; set; }
    public string Operation { get; set; } // INITIAL or WITHDRAW
    public decimal Amount { get; set; }
    public string TCRCode { get; set; }
    public string BusinessUnit { get; set; }
    public Operator? Operator { get; set; }
    
    // Fiscalization
    public string? FCDC { get; set; }
    public bool IsFiscalized { get; set; }
}