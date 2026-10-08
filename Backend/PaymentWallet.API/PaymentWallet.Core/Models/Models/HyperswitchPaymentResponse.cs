using System;
using System.Collections.Generic;
using System.Text;
namespace PaymentWallet.Core.Models
{
    public class HyperswitchPaymentResponse
    {
        public string? PaymentId { get; set; }
        public string? Status { get; set; }
        public int? Amount { get; set; }
        public string? Currency { get; set; }
        public string? ClientSecret { get; set; }
        public string? Error { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

