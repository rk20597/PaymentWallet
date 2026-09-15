namespace PaymentWallet.API.Models
{
	public class Accounts
	{
		public int AccountID { get; set; }
		public int UserID { get; set; }
		public string? AccountName { get; set; }
		public string? CreatedDate{ get; set; }
		public string? Status{ get; set; }
	}
}