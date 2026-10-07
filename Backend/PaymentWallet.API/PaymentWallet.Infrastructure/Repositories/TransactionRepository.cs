using OfficeOpenXml;
using PaymentWallet.Core.Interfaces;
using PaymentWallet.Core.Models;
using PaymentWallet.Infrastructure.Data;

namespace PaymentWallet.Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ExcelContext _context;

        public TransactionRepository(ExcelContext context)
        {
            _context = context;
        }

        public async Task<List<Transaction>>
            GetAllTransactionsByWallet(int walletID)
        {
            await _context.Lock.WaitAsync();
            try
            {
                var list = new List<Transaction>();
                using var package = _context.GetPackage();

                ExcelWorksheet? sheet = null;
                foreach (var ws in
                    package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Transaction")
                    {
                        sheet = ws;
                        break;
                    }
                }

                if (sheet == null) return list;
                if (sheet.Dimension == null) return list;

                for (int row = 2; row <= sheet
                    .Dimension.End.Row; row++)
                {
                    var wId = Convert.ToInt32(
                        sheet.Cells[row, 2].Value ?? 0);
                    if (wId != walletID) continue;

                    list.Add(new Transaction
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
                            sheet.Cells[row, 8].Value ?? 0),
                        BalanceBefore = Convert.ToDecimal(
                            sheet.Cells[row, 9].Value ?? 0),
                        BalanceAfter = Convert.ToDecimal(
                            sheet.Cells[row, 10].Value ?? 0)
                    });
                }
                return list;
            }
            finally
            {
                _context.Lock.Release();
            }
        }

        public async Task AddTransaction(
            Transaction transaction)
        {
            await _context.Lock.WaitAsync();
            try
            {
                using var package = _context.GetPackage();

                ExcelWorksheet? sheet = null;
                foreach (var ws in
                    package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Transaction")
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
                sheet.Cells[newRow, 2].Value =
                    transaction.WalletID;
                sheet.Cells[newRow, 3].Value =
                    transaction.Type;
                sheet.Cells[newRow, 4].Value =
                    transaction.Amount;
                sheet.Cells[newRow, 5].Value =
                    transaction.Description;
                sheet.Cells[newRow, 6].Value =
                    transaction.Date;
                sheet.Cells[newRow, 7].Value =
                    transaction.Status;
                sheet.Cells[newRow, 8].Value =
                    transaction.FundingMethodID;
                sheet.Cells[newRow, 9].Value =
                    transaction.BalanceBefore;
                sheet.Cells[newRow, 10].Value =
                    transaction.BalanceAfter;

                await package.SaveAsync();
            }
            finally
            {
                _context.Lock.Release();
            }
        }
    }
}

