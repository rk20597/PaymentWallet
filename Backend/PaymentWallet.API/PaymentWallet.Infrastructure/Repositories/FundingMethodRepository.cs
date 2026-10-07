using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions;
using PaymentWallet.Core.Interfaces;
using PaymentWallet.Core.Models;
using PaymentWallet.Infrastructure.Data;

namespace PaymentWallet.Infrastructure.Repositories
{
    public class FundingMethodRepository :
        IFundingMethodRepository
    {
        private readonly ExcelContext _context;

        public FundingMethodRepository(ExcelContext context)
        {
            _context = context;
        }

        public async Task<List<FundingMethods>>
            GetFundingMethodsByUser(int userID)
        {
            await _context.Lock.WaitAsync();
            try
            {
                var list = new List<FundingMethods>();
                using var package = _context.GetPackage();

                ExcelWorksheet? sheet = null;
                foreach (var ws in
                    package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "FundingMethods")
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
                    var uid = Convert.ToInt32(
                        sheet.Cells[row, 2].Value ?? 0);
                    if (uid != userID) continue;

                    list.Add(new FundingMethods
                    {
                        FundingMethodID = Convert.ToInt32(
                            sheet.Cells[row, 1].Value ?? 0),
                        UserID = uid,
                        Type = sheet.Cells[row, 3]
                            .Value?.ToString(),
                        MaskedDetails = sheet.Cells[row, 4]
                            .Value?.ToString(),
                        IsActive = sheet.Cells[row, 5]
                            .Value?.ToString()
                            ?.ToUpper() == "TRUE",
                        CreatedDate = sheet.Cells[row, 6]
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

        public async Task AddFundingMethod(
            FundingMethods method)
        {
            await _context.Lock.WaitAsync();
            try
            {
                using var package = _context.GetPackage();

                ExcelWorksheet? sheet = null;
                foreach (var ws in
                    package.Workbook.Worksheets)
                {
                    if (ws.Name.Trim() == "FundingMethods")
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
                sheet.Cells[newRow, 2].Value = method.UserID;
                sheet.Cells[newRow, 3].Value = method.Type;
                sheet.Cells[newRow, 4].Value =
                    method.MaskedDetails;
                sheet.Cells[newRow, 5].Value = "TRUE";
                sheet.Cells[newRow, 6].Value =
                    method.CreatedDate;

                await package.SaveAsync();
            }
            finally
            {
                _context.Lock.Release();
            }
        }
    }
}

