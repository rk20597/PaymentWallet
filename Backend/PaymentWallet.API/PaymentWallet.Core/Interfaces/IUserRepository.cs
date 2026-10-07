using System;
using System.Collections.Generic;
using System.Text;

using PaymentWallet.Core.Models;

namespace PaymentWallet.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<List<Users>> GetAllUsers();
        Task<Users?> GetUserByUsername(string username);
        Task AddUser(Users user);
    }
}

