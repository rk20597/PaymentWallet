namespace PaymentWallet.API.Models
{
    public class Notification
    {
       public int NotificationID { get; set; }
       public int UserID { get; set; }
       public string? Type { get; set; }
       public string? Message { get; set; }
       public string? Date { get; set; }
       public bool IsRead { get; set; }
    }
}