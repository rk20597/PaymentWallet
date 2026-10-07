using OfficeOpenXml;
using PaymentWallet.Core.Interfaces;
using PaymentWallet.Core.Models;
using PaymentWallet.Infrastructure.Data;

namespace PaymentWallet.Infrastructure.Repositories
{
    public class WalletRepository : IWalletRepository
    {
        private readonly ExcelContext _context;

        public WalletRepository(ExcelContext context)
        {
            _context = context;
        }

        public async Task<List<Wallet>> GetAllWallets()
        {
            await _context.Lock.WaitAsync();
            try
            {
                var wallets = new List<Wallet>();
                using var package = _context.GetPackage();

                ExcelWorksheet? sheet = null;
                foreach (var ws in
                    package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Wallet")
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
                _context.Lock.Release();
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
            await _context.Lock.WaitAsync();
            try
            {
                using var package = _context.GetPackage();

                ExcelWorksheet? sheet = null;
                foreach (var ws in
                    package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Wallet")
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
                sheet.Cells[newRow, 2].Value = wallet.AccountID;
                sheet.Cells[newRow, 3].Value = wallet.UserID;
                sheet.Cells[newRow, 4].Value = wallet.Balance;
                sheet.Cells[newRow, 5].Value = wallet.Currency;
                sheet.Cells[newRow, 6].Value =
                    wallet.CreatedDate;
                sheet.Cells[newRow, 7].Value = wallet.Status;

                await package.SaveAsync();
            }
            finally
            {
                _context.Lock.Release();
            }
        }

        public async Task UpdateWalletBalance(
            int walletID, decimal newBalance)
        {
            await _context.Lock.WaitAsync();
            try
            {
                using var package = _context.GetPackage();

                ExcelWorksheet? sheet = null;
                foreach (var ws in
                    package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Wallet")
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
                    int.TryParse(sheet.Cells[row, 1]
                        .Value?.ToString(), out int id);
                    if (id == walletID)
                    {
                        sheet.Cells[row, 4].Value =
                            newBalance;
                        break;
                    }
                }
                await package.SaveAsync();
            }
            finally
            {
                _context.Lock.Release();
            }
        }
    }
}

