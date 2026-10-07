using System;
using System.Collections.Generic;
using System.Text;

using OfficeOpenXml;

namespace PaymentWallet.Infrastructure.Data
{
    public class ExcelContext
    {
        private readonly string _filePath;
        public readonly SemaphoreSlim Lock =
            new SemaphoreSlim(1, 1);

        public ExcelContext(string filePath)
        {
            _filePath = filePath;
            ExcelPackage.License
                .SetNonCommercialPersonal(
                    "PaymentWallet");
        }

        public ExcelPackage GetPackage()
        {
            return new ExcelPackage(
                new FileInfo(_filePath));
        }

        public string FilePath => _filePath;
    }
}

