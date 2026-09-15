namespace PaymentWallet.API.Models
{
  public class Transaction
    {
        public int TransactionID { get; set; }
        public int WalletID { get; set; }
        public string? Type { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string? Date { get; set; }
        public string? Status { get; set; }
        public int FundingMethodID { get; set; }
    }
}