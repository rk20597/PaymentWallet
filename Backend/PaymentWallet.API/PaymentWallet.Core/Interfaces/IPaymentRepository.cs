using System;
using System.Collections.Generic;
using System.Text;

using PaymentWallet.Core.Models;

namespace PaymentWallet.Core.Interfaces
{
    public interface IPaymentRepository
    {
        Task<List<Payments>> GetAllPayments();
        Task<List<Payments>> GetPaymentsByWallet(
            int walletID);
        Task AddPayment(Payments payment);
        Task UpdatePaymentStatus(
            int paymentID, string status);
    }
}

