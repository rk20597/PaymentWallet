using PaymentWallet.Core.Models;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace PaymentWallet.Core.Interfaces
{
    public interface IAccountRepository
    {
        Task<List<Accounts>> GetAllAccounts();
        Task<List<Accounts>> GetAccountsByUser(int userID);
        Task AddAccount(Accounts account);
    }
}

