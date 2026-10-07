using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml.FormulaParsing.Excel.Functions;
using PaymentWallet.Core.Models;
using PaymentWallet.API.Repositories;
using System.Security.Claims;
using PaymentWallet.Core.Interfaces;

namespace PaymentWallet.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FundingMethodController : ControllerBase
    {
        private readonly IFundingMethodRepository _fundingRepo;
        private readonly IUserRepository _userRepo;
        private readonly ILogger<AccountsController> _logger;

        public FundingMethodController(
            IFundingMethodRepository fundingRepo,
            IUserRepository userRepo,
            ILogger<AccountsController> logger)
        {
            _fundingRepo = fundingRepo;
            _userRepo = userRepo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyMethods()
        {
            var userID = await GetCurrentUserID();
            if (userID == 0) return Unauthorized();

            var methods = await _fundingRepo
                .GetFundingMethodsByUser(userID);
            return Ok(methods);
        }

        [HttpPost]
        public async Task<IActionResult> AddMethod(
            [FromBody] AddFundingMethodRequest request)
        {
            if (string.IsNullOrEmpty(request.Type))
                return BadRequest(new
                {
                    message = "Type is required"
                });

            var userID = await GetCurrentUserID();
            if (userID == 0) return Unauthorized();

            // Mask card/account details
            string maskedDetails = "";
            if (request.Type == "Card")
            {
                if (string.IsNullOrEmpty(request.CardNumber) ||
                    request.CardNumber.Length < 4)
                    return BadRequest(new
                    {
                        message = "Invalid card number"
                    });
                maskedDetails = "**** **** **** " +
                    request.CardNumber
                    .Substring(request.CardNumber.Length - 4);
            }
            else if (request.Type == "BankAccount")
            {
                if (string.IsNullOrEmpty(request.AccountNumber))
                    return BadRequest(new
                    {
                        message = "Account number required"
                    });
                maskedDetails = "****" +
                    request.AccountNumber
                    .Substring(request.AccountNumber.Length - 4);
            }
            else if (request.Type == "UPI")
            {
                if (string.IsNullOrEmpty(request.UpiId))
                    return BadRequest(new
                    {
                        message = "UPI ID required"
                    });
                maskedDetails = request.UpiId;
            }

            var method = new FundingMethods
            {
                UserID = userID,
                Type = request.Type,
                MaskedDetails = maskedDetails,
                IsActive = true,
                CreatedDate = DateTime.Now
                    .ToString("dd-MM-yyyy")
            };

            await _fundingRepo.AddFundingMethod(method);
            return Ok(new
            {
                message = "Funding method added successfully"
            });
        }

        private async Task<int> GetCurrentUserID()
        {
            var username = User.FindFirst(
                ClaimTypes.Name)?.Value;
            var users = await _userRepo.GetAllUsers();
            var user = users.FirstOrDefault(u =>
                u.UserName == username);
            return user?.UserID ?? 0;
        }
    }

    public class AddFundingMethodRequest
    {
        public string? Type { get; set; }
        public string? CardNumber { get; set; }
        public string? AccountNumber { get; set; }
        public string? UpiId { get; set; }
        public string? ExpiryDate { get; set; }
    }
}

