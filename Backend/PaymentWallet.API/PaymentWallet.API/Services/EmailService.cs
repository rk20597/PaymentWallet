using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using PaymentWallet.Core.Interfaces;

namespace PaymentWallet.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IConfiguration config,
            ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(
    string toEmail,
    string subject,
    string body)
        {
            try
            {
                _logger.LogInformation(
                    "EMAIL NOTIFICATION | TO: {To} | SUBJECT: {Subject}",
                    toEmail, subject);

                await Task.Delay(100);

                _logger.LogInformation(
                    "Email notification logged successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Email error: {Error}", ex.Message);
            }
        }


        public async Task SendPaymentSentEmail(
            string toEmail, decimal amount,
            string currency, int receiverWalletID)
        {
            var subject = "Payment Sent - PayWallet";
            var body = $@"
                <h2>Payment Sent Successfully</h2>
                <p>Your payment of <strong>
                {currency} {amount:F2}</strong> 
                has been sent to Wallet #{receiverWalletID}.</p>
                <p>Thank you for using PayWallet.</p>";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendPaymentReceivedEmail(
            string toEmail, decimal amount,
            string currency, int senderWalletID)
        {
            var subject = "Payment Received - PayWallet";
            var body = $@"
                <h2>Payment Received</h2>
                <p>You have received <strong>
                {currency} {amount:F2}</strong> 
                from Wallet #{senderWalletID}.</p>
                <p>Thank you for using PayWallet.</p>";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendMoneyAddedEmail(
            string toEmail, decimal amount,
            string currency, decimal newBalance)
        {
            var subject = "Money Added - PayWallet";
            var body = $@"
                <h2>Money Added Successfully</h2>
                <p>You have added <strong>
                {currency} {amount:F2}</strong> 
                to your wallet.</p>
                <p>New Balance: <strong>
                {currency} {newBalance:F2}</strong></p>
                <p>Thank you for using PayWallet.</p>";
            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendRefundEmail(
            string toEmail, decimal amount,
            string currency)
        {
            var subject = "Refund Processed - PayWallet";
            var body = $@"
                <h2>Refund Processed</h2>
                <p>A refund of <strong>
                {currency} {amount:F2}</strong> 
                has been processed to your wallet.</p>
                <p>Thank you for using PayWallet.</p>";
            await SendEmailAsync(toEmail, subject, body);
        }
    }
}

