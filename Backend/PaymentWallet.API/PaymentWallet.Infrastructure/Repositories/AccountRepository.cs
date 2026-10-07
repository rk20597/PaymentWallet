using OfficeOpenXml;
using PaymentWallet.Core.Interfaces;
using PaymentWallet.Core.Models;
using PaymentWallet.Infrastructure.Data;

namespace PaymentWallet.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly ExcelContext _context;

        public AccountRepository(ExcelContext context)
        {
            _context = context;
        }

        public async Task<List<Accounts>> GetAllAccounts()
        {
            await _context.Lock.WaitAsync();
            try
            {
                var accounts = new List<Accounts>();
                using var package = _context.GetPackage();

                ExcelWorksheet? sheet = null;
                foreach (var ws in
                    package.Workbook.Worksheets)
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
                _context.Lock.Release();
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
            await _context.Lock.WaitAsync();
            try
            {
                using var package = _context.GetPackage();

                ExcelWorksheet? sheet = null;
                foreach (var ws in
                    package.Workbook.Worksheets)
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
                        int.TryParse(sheet.Cells[r, 1]
                            .Value?.ToString(), out int id);
                        if (id > maxId) maxId = id;
                    }
                }

                int newRow =
                    (sheet.Dimension?.End?.Row ?? 1) + 1;
                sheet.Cells[newRow, 1].Value = maxId + 1;
                sheet.Cells[newRow, 2].Value = account.UserID;
                sheet.Cells[newRow, 3].Value =
                    account.AccountName;
                sheet.Cells[newRow, 4].Value =
                    account.CreatedDate;
                sheet.Cells[newRow, 5].Value = account.Status;

                await package.SaveAsync();
            }
            finally
            {
                _context.Lock.Release();
            }
        }
    }
}

