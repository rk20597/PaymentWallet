namespace PaymentWallet.API.Models
{
    public class Users
    {
        public int UserID { get; set; }
        public string? UserName { get; set; }
        public string? PasswordHash { get; set; }
        public string? Role { get; set; }
        public bool IsActive { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? CreatedDate { get; set; }
    }
}