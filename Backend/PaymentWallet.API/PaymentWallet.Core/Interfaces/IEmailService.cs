using System;
using System.Collections.Generic;
using System.Text;
namespace PaymentWallet.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(
            string toEmail,
            string subject,
            string body);

        Task SendPaymentSentEmail(
            string toEmail,
            decimal amount,
            string currency,
            int receiverWalletID);

        Task SendPaymentReceivedEmail(
            string toEmail,
            decimal amount,
            string currency,
            int senderWalletID);

        Task SendMoneyAddedEmail(
            string toEmail,
            decimal amount,
            string currency,
            decimal newBalance);

        Task SendRefundEmail(
            string toEmail,
            decimal amount,
            string currency);
    }
}

