using System;
using System.Collections.Generic;
using System.Text;

using PaymentWallet.Core.Models;

namespace PaymentWallet.Core.Interfaces
{
    public interface ITransactionRepository
    {
        Task<List<Transaction>>
            GetAllTransactionsByWallet(int walletID);
        Task AddTransaction(Transaction transaction);
    }
}

