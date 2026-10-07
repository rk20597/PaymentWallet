using System;
using System.Collections.Generic;
using System.Text;

using PaymentWallet.Core.Models;

namespace PaymentWallet.Core.Interfaces
{
    public interface IWalletRepository
    {
        Task<List<Wallet>> GetAllWallets();
        Task<List<Wallet>> GetWalletsByUser(int userID);
        Task AddWallet(Wallet wallet);
        Task UpdateWalletBalance(
            int walletID, decimal newBalance);
    }
}

