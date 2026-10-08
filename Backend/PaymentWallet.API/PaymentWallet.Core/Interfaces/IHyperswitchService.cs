using System;
using System.Collections.Generic;
using System.Text;
using PaymentWallet.Core.Models;

namespace PaymentWallet.Core.Interfaces
{
    public interface IHyperswitchService
    {
        Task<HyperswitchPaymentResponse> CreatePayment(
            decimal amount,
            string currency,
            string description);

        Task<HyperswitchPaymentResponse> ConfirmPayment(
            string paymentId);

        Task<HyperswitchPaymentResponse> CapturePayment(
            string paymentId,
            decimal amount);
    }
}

