namespace PaymentWallet.API.Models
{
    public class Wallet
    {
        public int WalletID { get; set; }
        public int AccountID { get; set; }
        public int UserID { get; set; }
        public decimal Balance { get; set; }
        public string? Currency { get; set; }
        public string? CreatedDate { get; set; }
        public string? Status { get; set; }
    }
}