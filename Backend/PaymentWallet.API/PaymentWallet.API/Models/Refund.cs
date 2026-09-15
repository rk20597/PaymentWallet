namespace PaymentWallet.API.Models
{
public class Refund
    {
        public int RefundID { get; set; }
        public int PaymentID { get; set; }
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
        public string? Status { get; set; }
        public string? Date { get; set; }
        
    }
}