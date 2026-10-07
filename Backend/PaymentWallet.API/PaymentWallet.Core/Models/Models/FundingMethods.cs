namespace PaymentWallet.Core.Models
{
   public class FundingMethods
    {
        public int FundingMethodID { get; set; }
        public int UserID { get; set; }
        public string? Type { get; set; }
        public string? MaskedDetails { get; set; }
        public bool IsActive { get; set; }
        public string? CreatedDate { get; set; }
    }
}