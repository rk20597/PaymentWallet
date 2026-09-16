using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentWallet.API.Models;
using PaymentWallet.API.Repositories;
using System.Security.Claims;
using System.Security.Principal;

namespace PaymentWallet.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly PaymentWalletRepository _repo;

        public AccountsController(
            PaymentWalletRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAccounts()
        {
            var username = User.FindFirst(
                ClaimTypes.Name)?.Value;
            var users = await _repo.GetAllUsers();
            var user = users.FirstOrDefault(u =>
                u.UserName == username);
            if (user == null)
                return Unauthorized();

            var accounts = await _repo
                .GetAccountsByUser(user.UserID);
            return Ok(accounts);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAccounts()
        {
            var accounts = await _repo.GetAllAccounts();
            return Ok(accounts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount(
            [FromBody] Accounts account)
        {
            var username = User.FindFirst(
                ClaimTypes.Name)?.Value;
            var users = await _repo.GetAllUsers();
            var user = users.FirstOrDefault(u =>
                u.UserName == username);
            if (user == null)
                return Unauthorized();

            account.UserID = user.UserID;
            account.CreatedDate = DateTime.Now
                .ToString("dd-MM-yyyy");
            account.Status = "Active";

            await _repo.AddAccount(account);
            return Ok(new
            {
                message = "Account created successfully"
            });
        }
    }
}

