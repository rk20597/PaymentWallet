using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentWallet.API.Models;
using PaymentWallet.API.Repositories;
using System.Security.Claims;

namespace PaymentWallet.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly PaymentWalletRepository _repo;
        private readonly ILogger<AccountsController> _logger;

        public WalletController(
            PaymentWalletRepository repo,
            ILogger<AccountsController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyWallets()
        {
            var username = User.FindFirst(
                ClaimTypes.Name)?.Value;
            var users = await _repo.GetAllUsers();
            var user = users.FirstOrDefault(u =>
                u.UserName == username);
            if (user == null)
                return Unauthorized();

            var wallets = await _repo
                .GetWalletsByUser(user.UserID);
            return Ok(wallets);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllWallets()
        {
            var wallets = await _repo.GetAllWallets();
            return Ok(wallets);
        }

        [HttpPost]
        public async Task<IActionResult> CreateWallet(
            [FromBody] Wallet wallet)
        {
            var username = User.FindFirst(
                ClaimTypes.Name)?.Value;
            var users = await _repo.GetAllUsers();
            var user = users.FirstOrDefault(u =>
                u.UserName == username);
            if (user == null)
                return Unauthorized();

            // Verify account belongs to user
            var accounts = await _repo
                .GetAccountsByUser(user.UserID);
            var account = accounts.FirstOrDefault(a =>
                a.AccountID == wallet.AccountID);
            if (account == null)
                return BadRequest(new
                {
                    message = "Invalid account"
                });

            wallet.UserID = user.UserID;
            wallet.Balance = 0;
            wallet.CreatedDate = DateTime.Now
                .ToString("dd-MM-yyyy");
            wallet.Status = "Active";
            wallet.Currency = wallet.Currency ?? "INR";

            await _repo.AddWallet(wallet);
            return Ok(new
            {
                message = "Wallet created successfully"
            });
        }

        [HttpGet("{walletId}/balance")]
        public async Task<IActionResult> GetBalance(
            int walletId)
        {
            var username = User.FindFirst(
                ClaimTypes.Name)?.Value;
            var users = await _repo.GetAllUsers();
            var user = users.FirstOrDefault(u =>
                u.UserName == username);
            if (user == null)
                return Unauthorized();

            var wallets = await _repo
                .GetWalletsByUser(user.UserID);
            var wallet = wallets.FirstOrDefault(w =>
                w.WalletID == walletId);
            if (wallet == null)
                return NotFound(new
                {
                    message = "Wallet not found"
                });

            return Ok(new
            {
                walletID = wallet.WalletID,
                balance = wallet.Balance,
                currency = wallet.Currency,
                status = wallet.Status
            });
        }

        [HttpGet("{walletId}/transactions")]
        public async Task<IActionResult> GetTransactions(
            int walletId)
        {
            var username = User.FindFirst(
                ClaimTypes.Name)?.Value;
            var users = await _repo.GetAllUsers();
            var user = users.FirstOrDefault(u =>
                u.UserName == username);
            if (user == null)
                return Unauthorized();

            var wallets = await _repo
                .GetWalletsByUser(user.UserID);
            var wallet = wallets.FirstOrDefault(w =>
                w.WalletID == walletId);
            if (wallet == null)
                return NotFound(new
                {
                    message = "Wallet not found"
                });

            var transactions = await _repo
                .GetTransactionsByWallet(walletId);
            return Ok(transactions);
        }

        [HttpPost("{walletId}/addmoney")]
        public async Task<IActionResult> AddMoney(
    int walletId,
    [FromBody] AddMoneyRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest(new
                {
                    message = "Amount must be greater than 0"
                });

            var username = User.FindFirst(
                ClaimTypes.Name)?.Value;
            var users = await _repo.GetAllUsers();
            var user = users.FirstOrDefault(u =>
                u.UserName == username);
            if (user == null) return Unauthorized();

            var wallets = await _repo
                .GetWalletsByUser(user.UserID);
            var wallet = wallets.FirstOrDefault(w =>
                w.WalletID == walletId);
            if (wallet == null)
                return NotFound(new
                {
                    message = "Wallet not found"
                });

            // Verify funding method belongs to user
            var methods = await _repo
                .GetFundingMethodsByUser(user.UserID);
            var method = methods.FirstOrDefault(m =>
                m.FundingMethodID == request.FundingMethodID);
            if (method == null)
                return BadRequest(new
                {
                    message = "Invalid funding method"
                });

            // Update wallet balance
            decimal newBalance = wallet.Balance + request.Amount;
            await _repo.UpdateWalletBalance(walletId, newBalance);

            // Add transaction record
            var transaction = new Transaction
            {
                WalletID = walletId,
                Type = "Credit",
                Amount = request.Amount,
                Description = "Added money from " + method.Type +
                    " - " + method.MaskedDetails,
                Date = DateTime.Now.ToString("dd-MM-yyyy HH:mm"),
                Status = "Completed",
                FundingMethodID = request.FundingMethodID,
                BalanceBefore = wallet.Balance,
                BalanceAfter = newBalance
            };
            await _repo.AddTransaction(transaction);

            return Ok(new
            {
                message = "Money added successfully",
                newBalance = newBalance,
                transactionAmount = request.Amount
            });
        }

    }

    public class AddMoneyRequest
    {
        public decimal Amount { get; set; }
        public int FundingMethodID { get; set; }
    }
}

