using OfficeOpenXml;
using PaymentWallet.Core.Interfaces;
using PaymentWallet.Core.Models;
using PaymentWallet.Infrastructure.Data;

namespace PaymentWallet.Infrastructure.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ExcelContext _context;

        public PaymentRepository(ExcelContext context)
        {
            _context = context;
        }

        public async Task<List<Payments>> GetAllPayments()
        {
            await _context.Lock.WaitAsync();
            try
            {
                var list = new List<Payments>();
                using var package = _context.GetPackage();

                ExcelWorksheet? sheet = null;
                foreach (var ws in
                    package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Payments")
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
                    list.Add(new Payments
                    {
                        PaymentID = Convert.ToInt32(
                            sheet.Cells[row, 1].Value ?? 0),
                        SenderWalletID = Convert.ToInt32(
                            sheet.Cells[row, 2].Value ?? 0),
                        ReceiverWalletID = Convert.ToInt32(
                            sheet.Cells[row, 3].Value ?? 0),
                        Amount = Convert.ToDecimal(
                            sheet.Cells[row, 4].Value ?? 0),
                        Status = sheet.Cells[row, 5]
                            .Value?.ToString(),
                        HyperSwitchPaymentID =
                            sheet.Cells[row, 6]
                            .Value?.ToString(),
                        AuthorizedDate = sheet.Cells[row, 7]
                            .Value?.ToString(),
                        CapturedDate = sheet.Cells[row, 8]
                            .Value?.ToString(),
                        Description = sheet.Cells[row, 9]
                            .Value?.ToString()
                    });
                }
                return list;
            }
            finally
            {
                _context.Lock.Release();
            }
        }

        public async Task<List<Payments>> GetPaymentsByWallet(
            int walletID)
        {
            var payments = await GetAllPayments();
            return payments.Where(p =>
                p.SenderWalletID == walletID ||
                p.ReceiverWalletID == walletID).ToList();
        }

        public async Task AddPayment(Payments payment)
        {
            await _context.Lock.WaitAsync();
            try
            {
                using var package = _context.GetPackage();

                ExcelWorksheet? sheet = null;
                foreach (var ws in
                    package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Payments")
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
                    payment.SenderWalletID;
                sheet.Cells[newRow, 3].Value =
                    payment.ReceiverWalletID;
                sheet.Cells[newRow, 4].Value = payment.Amount;
                sheet.Cells[newRow, 5].Value = payment.Status;
                sheet.Cells[newRow, 6].Value =
                    payment.HyperSwitchPaymentID;
                sheet.Cells[newRow, 7].Value =
                    payment.AuthorizedDate;
                sheet.Cells[newRow, 8].Value =
                    payment.CapturedDate;
                sheet.Cells[newRow, 9].Value =
                    payment.Description;

                await package.SaveAsync();
            }
            finally
            {
                _context.Lock.Release();
            }
        }

        public async Task UpdatePaymentStatus(
            int paymentID, string status)
        {
            await _context.Lock.WaitAsync();
            try
            {
                using var package = _context.GetPackage();

                ExcelWorksheet? sheet = null;
                foreach (var ws in
                    package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "Payments")
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
                    if (id == paymentID)
                    {
                        sheet.Cells[row, 5].Value = status;
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

