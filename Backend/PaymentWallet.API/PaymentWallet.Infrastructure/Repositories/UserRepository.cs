using OfficeOpenXml;
using PaymentWallet.Core.Interfaces;
using PaymentWallet.Core.Models;
using PaymentWallet.Infrastructure.Data;

namespace PaymentWallet.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ExcelContext _context;

        public UserRepository(ExcelContext context)
        {
            _context = context;
        }

        public async Task<List<Users>> GetAllUsers()
        {
            await _context.Lock.WaitAsync();
            try
            {
                var users = new List<Users>();
                using var package = _context.GetPackage();

                ExcelWorksheet? sheet = null;
                foreach (var ws in
                    package.Workbook.Worksheets)
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
                            .Value?.ToString()
                            ?.ToUpper() == "TRUE",
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
                _context.Lock.Release();
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
            await _context.Lock.WaitAsync();
            try
            {
                using var package = _context.GetPackage();

                ExcelWorksheet? sheet = null;
                foreach (var ws in
                    package.Workbook.Worksheets)
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
                        int.TryParse(sheet.Cells[r, 1]
                            .Value?.ToString(), out int id);
                        if (id > maxId) maxId = id;
                    }
                }

                int newRow =
                    (sheet.Dimension?.End?.Row ?? 1) + 1;
                sheet.Cells[newRow, 1].Value = maxId + 1;
                sheet.Cells[newRow, 2].Value = user.UserName;
                sheet.Cells[newRow, 3].Value =
                    user.PasswordHash;
                sheet.Cells[newRow, 4].Value = user.Role;
                sheet.Cells[newRow, 5].Value =
                    user.IsActive ? "TRUE" : "FALSE";
                sheet.Cells[newRow, 6].Value = user.FullName;
                sheet.Cells[newRow, 7].Value = user.Phone;
                sheet.Cells[newRow, 8].Value =
                    user.CreatedDate;

                await package.SaveAsync();
            }
            finally
            {
                _context.Lock.Release();
            }
        }
    }
}

