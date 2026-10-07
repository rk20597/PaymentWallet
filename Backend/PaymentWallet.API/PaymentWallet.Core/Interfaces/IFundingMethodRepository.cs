using System;
using System.Collections.Generic;
using System.Text;

using PaymentWallet.Core.Models;

namespace PaymentWallet.Core.Interfaces
{
    public interface IFundingMethodRepository
    {
        Task<List<FundingMethods>>
            GetFundingMethodsByUser(int userID);
        Task AddFundingMethod(FundingMethods method);
    }
}

