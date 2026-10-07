using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentWallet.Core.Models;
using PaymentWallet.API.Repositories;
using System.Security.Claims;
using System.Security.Principal;
using PaymentWallet.Core.Interfaces;

namespace PaymentWallet.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountRepository _accountRepo;
        private readonly IUserRepository _userRepo;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(
            IAccountRepository accountRepo,
            IUserRepository userRepo,
            ILogger<AccountsController> logger)
        {
            _accountRepo = accountRepo;
            _userRepo = userRepo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAccounts()
        {
            var username = User.FindFirst(
                ClaimTypes.Name)?.Value;
            var users = await _userRepo.GetAllUsers();
            var user = users.FirstOrDefault(u =>
                u.UserName == username);
            if (user == null)
                return Unauthorized();

            var accounts = await _accountRepo
                .GetAccountsByUser(user.UserID);
            return Ok(accounts);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAccounts()
        {
            var accounts = await _accountRepo.GetAllAccounts();
            return Ok(accounts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount(
            [FromBody] Accounts account)
        {
            var username = User.FindFirst(
                ClaimTypes.Name)?.Value;
            var users = await _userRepo.GetAllUsers();
            var user = users.FirstOrDefault(u =>
                u.UserName == username);
            if (user == null)
                return Unauthorized();

            account.UserID = user.UserID;
            account.CreatedDate = DateTime.Now
                .ToString("dd-MM-yyyy");
            account.Status = "Active";

            await _accountRepo.AddAccount(account);
            return Ok(new
            {
                message = "Account created successfully"
            });
        }
    }
}

