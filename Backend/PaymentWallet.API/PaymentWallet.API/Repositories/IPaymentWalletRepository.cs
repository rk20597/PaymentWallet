using OfficeOpenXml.FormulaParsing.Excel.Functions;
using PaymentWallet.Core.Models;
using System.Security.Principal;

namespace PaymentWallet.API.Repositories
{
    public interface IPaymentWalletRepository
    {
        // Users
        Task<List<Users>> GetAllUsers();
        Task<Users?> GetUserByUsername(string username);
        Task AddUser(Users user);

        // Accounts
        Task<List<Accounts>> GetAllAccounts();
        Task<List<Accounts>> GetAccountsByUser(int userID);
        Task AddAccount(Accounts account);

        // Wallets
        Task<List<Wallet>> GetAllWallets();
        Task<List<Wallet>> GetWalletsByUser(int userID);
        Task AddWallet(Wallet wallet);
        Task UpdateWalletBalance(
            int walletID, decimal newBalance);

        // Transactions
        Task<List<Transaction>> GetAllTransactionsByWallet(
            int walletID);
        Task AddTransaction(Transaction transaction);

        // Funding Methods
        Task<List<FundingMethods>> GetFundingMethodsByUser(
            int userID);
        Task AddFundingMethod(FundingMethods method);
    }
}

