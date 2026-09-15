using OfficeOpenXml;
using PaymentWallet.API.Models;
using System.IO;
using System.Security.Principal;

namespace PaymentWallet.API.Repositories
{
    public class PaymentWalletRepository
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _lock =
            new SemaphoreSlim(1, 1);

        public PaymentWalletRepository(string filePath)
        {
            _filePath = filePath;
            ExcelPackage.License
                .SetNonCommercialPersonal("PaymentWallet");
        }

        // ==================
        // USERS
        // ==================

        public async Task<List<Users>> GetAllUsers()
        {
            await _lock.WaitAsync();
            try
            {
                var users = new List<Users>();
                using var package = new ExcelPackage(
                    new FileInfo(_filePath));

                ExcelWorksheet? sheet = null;
                foreach (var ws in package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Users")
                    {
                        sheet = ws;
                        break;
                    }
                }

                if (sheet == null) return users;
                if (sheet.Dimension == null) return users;

                for (int row = 2; row <= sheet
                    .Dimension.End.Row; row++)
                {
                    var userName = sheet.Cells[row, 2]
                        .Value?.ToString();
                    if (string.IsNullOrEmpty(userName))
                        continue;

                    users.Add(new Users
                    {
                        UserID = Convert.ToInt32(
                            sheet.Cells[row, 1].Value ?? 0),
                        UserName = userName,
                        PasswordHash = sheet.Cells[row, 3]
                            .Value?.ToString(),
                        Role = sheet.Cells[row, 4]
                            .Value?.ToString(),
                        IsActive = sheet.Cells[row, 5]
                            .Value?.ToString() == "TRUE",
                        FullName = sheet.Cells[row, 6]
                            .Value?.ToString(),
                        Phone = sheet.Cells[row, 7]
                            .Value?.ToString(),
                        CreatedDate = sheet.Cells[row, 8]
                            .Value?.ToString()
                    });
                }
                return users;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<Users?> GetUserByUsername(
            string username)
        {
            var users = await GetAllUsers();
            return users.FirstOrDefault(u =>
                u.UserName?.ToLower() ==
                username.ToLower());
        }

        public async Task AddUser(Users user)
        {
            await _lock.WaitAsync();
            try
            {
                using var package = new ExcelPackage(
                    new FileInfo(_filePath));

                ExcelWorksheet? sheet = null;
                foreach (var ws in package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Users")
                    {
                        sheet = ws;
                        break;
                    }
                }

                if (sheet == null) return;

                int maxId = 0;
                if (sheet.Dimension != null)
                {
                    for (int r = 2; r <= sheet
                        .Dimension.End.Row; r++)
                    {
                        var idVal = sheet.Cells[r, 1]
                            .Value?.ToString();
                        int id = 0;
                        int.TryParse(idVal, out id);
                        if (id > maxId) maxId = id;
                    }
                }

                int newRow = (sheet.Dimension?.End?.Row ?? 1) + 1;
                sheet.Cells[newRow, 1].Value = maxId + 1;
                sheet.Cells[newRow, 2].Value = user.UserName;
                sheet.Cells[newRow, 3].Value = user.PasswordHash;
                sheet.Cells[newRow, 4].Value = user.Role;
                sheet.Cells[newRow, 5].Value =
                    user.IsActive ? "TRUE" : "FALSE";
                sheet.Cells[newRow, 6].Value = user.FullName;
                sheet.Cells[newRow, 7].Value = user.Phone;
                sheet.Cells[newRow, 8].Value = user.CreatedDate;

                await package.SaveAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        // ==================
        // ACCOUNTS
        // ==================

        public async Task<List<Accounts>> GetAllAccounts()
        {
            await _lock.WaitAsync();
            try
            {
                var accounts = new List<Accounts>();
                using var package = new ExcelPackage(
                    new FileInfo(_filePath));

                ExcelWorksheet? sheet = null;
                foreach (var ws in package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Accounts")
                    {
                        sheet = ws;
                        break;
                    }
                }

                if (sheet == null) return accounts;
                if (sheet.Dimension == null) return accounts;

                for (int row = 2; row <= sheet
                    .Dimension.End.Row; row++)
                {
                    var accountName = sheet.Cells[row, 3]
                        .Value?.ToString();
                    if (string.IsNullOrEmpty(accountName))
                        continue;

                    accounts.Add(new Accounts
                    {
                        AccountID = Convert.ToInt32(
                            sheet.Cells[row, 1].Value ?? 0),
                        UserID = Convert.ToInt32(
                            sheet.Cells[row, 2].Value ?? 0),
                        AccountName = accountName,
                        CreatedDate = sheet.Cells[row, 4]
                            .Value?.ToString(),
                        Status = sheet.Cells[row, 5]
                            .Value?.ToString()
                    });
                }
                return accounts;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<Accounts>> GetAccountsByUser(
            int userID)
        {
            var accounts = await GetAllAccounts();
            return accounts.Where(a =>
                a.UserID == userID).ToList();
        }

        public async Task AddAccount(Accounts account)
        {
            await _lock.WaitAsync();
            try
            {
                using var package = new ExcelPackage(
                    new FileInfo(_filePath));

                ExcelWorksheet? sheet = null;
                foreach (var ws in package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Accounts")
                    {
                        sheet = ws;
                        break;
                    }
                }

                if (sheet == null) return;

                int maxId = 0;
                if (sheet.Dimension != null)
                {
                    for (int r = 2; r <= sheet
                        .Dimension.End.Row; r++)
                    {
                        var idVal = sheet.Cells[r, 1]
                            .Value?.ToString();
                        int id = 0;
                        int.TryParse(idVal, out id);
                        if (id > maxId) maxId = id;
                    }
                }

                int newRow = (sheet.Dimension?.End?.Row ?? 1) + 1;
                sheet.Cells[newRow, 1].Value = maxId + 1;
                sheet.Cells[newRow, 2].Value = account.UserID;
                sheet.Cells[newRow, 3].Value = account.AccountName;
                sheet.Cells[newRow, 4].Value = account.CreatedDate;
                sheet.Cells[newRow, 5].Value = account.Status;

                await package.SaveAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        // ==================
        // WALLETS
        // ==================

        public async Task<List<Wallet>> GetAllWallets()
        {
            await _lock.WaitAsync();
            try
            {
                var wallets = new List<Wallet>();
                using var package = new ExcelPackage(
                    new FileInfo(_filePath));

                ExcelWorksheet? sheet = null;
                foreach (var ws in package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Wallets")
                    {
                        sheet = ws;
                        break;
                    }
                }

                if (sheet == null) return wallets;
                if (sheet.Dimension == null) return wallets;

                for (int row = 2; row <= sheet
                    .Dimension.End.Row; row++)
                {
                    wallets.Add(new Wallet
                    {
                        WalletID = Convert.ToInt32(
                            sheet.Cells[row, 1].Value ?? 0),
                        AccountID = Convert.ToInt32(
                            sheet.Cells[row, 2].Value ?? 0),
                        UserID = Convert.ToInt32(
                            sheet.Cells[row, 3].Value ?? 0),
                        Balance = Convert.ToDecimal(
                            sheet.Cells[row, 4].Value ?? 0),
                        Currency = sheet.Cells[row, 5]
                            .Value?.ToString(),
                        CreatedDate = sheet.Cells[row, 6]
                            .Value?.ToString(),
                        Status = sheet.Cells[row, 7]
                            .Value?.ToString()
                    });
                }
                return wallets;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<Wallet>> GetWalletsByUser(
            int userID)
        {
            var wallets = await GetAllWallets();
            return wallets.Where(w =>
                w.UserID == userID).ToList();
        }

        public async Task AddWallet(Wallet wallet)
        {
            await _lock.WaitAsync();
            try
            {
                using var package = new ExcelPackage(
                    new FileInfo(_filePath));

                ExcelWorksheet? sheet = null;
                foreach (var ws in package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Wallets")
                    {
                        sheet = ws;
                        break;
                    }
                }

                if (sheet == null) return;

                int maxId = 0;
                if (sheet.Dimension != null)
                {
                    for (int r = 2; r <= sheet
                        .Dimension.End.Row; r++)
                    {
                        var idVal = sheet.Cells[r, 1]
                            .Value?.ToString();
                        int id = 0;
                        int.TryParse(idVal, out id);
                        if (id > maxId) maxId = id;
                    }
                }

                int newRow = (sheet.Dimension?.End?.Row ?? 1) + 1;
                sheet.Cells[newRow, 1].Value = maxId + 1;
                sheet.Cells[newRow, 2].Value = wallet.AccountID;
                sheet.Cells[newRow, 3].Value = wallet.UserID;
                sheet.Cells[newRow, 4].Value = wallet.Balance;
                sheet.Cells[newRow, 5].Value = wallet.Currency;
                sheet.Cells[newRow, 6].Value = wallet.CreatedDate;
                sheet.Cells[newRow, 7].Value = wallet.Status;

                await package.SaveAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task UpdateWalletBalance(
            int walletID, decimal newBalance)
        {
            await _lock.WaitAsync();
            try
            {
                using var package = new ExcelPackage(
                    new FileInfo(_filePath));

                ExcelWorksheet? sheet = null;
                foreach (var ws in package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Wallets")
                    {
                        sheet = ws;
                        break;
                    }
                }

                if (sheet == null) return;
                if (sheet.Dimension == null) return;

                for (int row = 2; row <= sheet
                    .Dimension.End.Row; row++)
                {
                    var idVal = sheet.Cells[row, 1]
                        .Value?.ToString();
                    int id = 0;
                    int.TryParse(idVal, out id);
                    if (id == walletID)
                    {
                        sheet.Cells[row, 4].Value = newBalance;
                        break;
                    }
                }
                await package.SaveAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        // ==================
        // TRANSACTIONS
        // ==================

        public async Task<List<Transaction>>
            GetTransactionsByWallet(int walletID)
        {
            await _lock.WaitAsync();
            try
            {
                var transactions = new List<Transaction>();
                using var package = new ExcelPackage(
                    new FileInfo(_filePath));

                ExcelWorksheet? sheet = null;
                foreach (var ws in package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Transactions")
                    {
                        sheet = ws;
                        break;
                    }
                }

                if (sheet == null) return transactions;
                if (sheet.Dimension == null) return transactions;

                for (int row = 2; row <= sheet
                    .Dimension.End.Row; row++)
                {
                    var wId = Convert.ToInt32(
                        sheet.Cells[row, 2].Value ?? 0);
                    if (wId != walletID) continue;

                    transactions.Add(new Transaction
                    {
                        TransactionID = Convert.ToInt32(
                            sheet.Cells[row, 1].Value ?? 0),
                        WalletID = wId,
                        Type = sheet.Cells[row, 3]
                            .Value?.ToString(),
                        Amount = Convert.ToDecimal(
                            sheet.Cells[row, 4].Value ?? 0),
                        Description = sheet.Cells[row, 5]
                            .Value?.ToString(),
                        Date = sheet.Cells[row, 6]
                            .Value?.ToString(),
                        Status = sheet.Cells[row, 7]
                            .Value?.ToString(),
                        FundingMethodID = Convert.ToInt32(
                            sheet.Cells[row, 8].Value ?? 0)
                    });
                }
                return transactions;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task AddTransaction(Transaction transaction)
        {
            await _lock.WaitAsync();
            try
            {
                using var package = new ExcelPackage(
                    new FileInfo(_filePath));

                ExcelWorksheet? sheet = null;
                foreach (var ws in package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Transactions")
                    {
                        sheet = ws;
                        break;
                    }
                }

                if (sheet == null) return;

                int maxId = 0;
                if (sheet.Dimension != null)
                {
                    for (int r = 2; r <= sheet
                        .Dimension.End.Row; r++)
                    {
                        var idVal = sheet.Cells[r, 1]
                            .Value?.ToString();
                        int id = 0;
                        int.TryParse(idVal, out id);
                        if (id > maxId) maxId = id;
                    }
                }

                int newRow = (sheet.Dimension?.End?.Row ?? 1) + 1;
                sheet.Cells[newRow, 1].Value = maxId + 1;
                sheet.Cells[newRow, 2].Value = transaction.WalletID;
                sheet.Cells[newRow, 3].Value = transaction.Type;
                sheet.Cells[newRow, 4].Value = transaction.Amount;
                sheet.Cells[newRow, 5].Value = transaction.Description;
                sheet.Cells[newRow, 6].Value = transaction.Date;
                sheet.Cells[newRow, 7].Value = transaction.Status;
                sheet.Cells[newRow, 8].Value =
                    transaction.FundingMethodID;

                await package.SaveAsync();
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}

