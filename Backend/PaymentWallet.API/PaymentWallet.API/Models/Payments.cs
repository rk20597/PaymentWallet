namespace PaymentWallet.API.Models
{
public class Payments
	{
		public int PaymentID { get; set; }
		public int SenderWalletID { get; set; }
		public int ReceiverWalletID { get; set; }
		public decimal Amount { get; set; }
		public string? Status { get; set; }
		public string? HyperSwitchPaymentID { get; set; }
		public string? AuthorizedDate { get; set; }
		public string? CapturedDate { get; set; }
		public string? Description { get; set; }
		
	}
}