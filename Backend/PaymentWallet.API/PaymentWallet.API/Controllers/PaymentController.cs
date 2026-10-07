using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentWallet.Core.Interfaces;
using PaymentWallet.Core.Models;
using System.Security.Claims;

namespace PaymentWallet.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IWalletRepository _walletRepo;
        private readonly IUserRepository _userRepo;
        private readonly ITransactionRepository _txRepo;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentRepository paymentRepo,
            IWalletRepository walletRepo,
            IUserRepository userRepo,
            ITransactionRepository txRepo,
            ILogger<PaymentController> logger)
        {
            _paymentRepo = paymentRepo;
            _walletRepo = walletRepo;
            _userRepo = userRepo;
            _txRepo = txRepo;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendPayment(
            [FromBody] SendPaymentRequest request)
        {
            if (request.Amount <= 0)
                return BadRequest(new
                {
                    message = "Amount must be greater than 0"
                });

            var userID = await GetCurrentUserID();
            if (userID == 0) return Unauthorized();

            // Verify sender wallet belongs to user
            var senderWallets = await _walletRepo
                .GetWalletsByUser(userID);
            var senderWallet = senderWallets
                .FirstOrDefault(w =>
                w.WalletID == request.SenderWalletID);
            if (senderWallet == null)
                return BadRequest(new
                {
                    message = "Invalid sender wallet"
                });

            // Check sufficient balance
            if (senderWallet.Balance < request.Amount)
                return BadRequest(new
                {
                    message = "Insufficient balance"
                });

            // Get receiver wallet
            var allWallets = await _walletRepo
                .GetAllWallets();
            var receiverWallet = allWallets
                .FirstOrDefault(w =>
                w.WalletID == request.ReceiverWalletID);
            if (receiverWallet == null)
                return BadRequest(new
                {
                    message = "Invalid receiver wallet"
                });

            // Currency validation
            if (senderWallet.Currency !=
                receiverWallet.Currency)
                return BadRequest(new
                {
                    message = "Currency mismatch between wallets"
                });

            // Cannot send to same wallet
            if (request.SenderWalletID ==
                request.ReceiverWalletID)
                return BadRequest(new
                {
                    message = "Cannot send to same wallet"
                });

            // Create payment record
            var payment = new Payments
            {
                SenderWalletID = request.SenderWalletID,
                ReceiverWalletID = request.ReceiverWalletID,
                Amount = request.Amount,
                Status = "Completed",
                HyperSwitchPaymentID = "LOCAL_" +
                    Guid.NewGuid().ToString("N")[..8],
                AuthorizedDate = DateTime.Now
                    .ToString("dd-MM-yyyy HH:mm"),
                CapturedDate = DateTime.Now
                    .ToString("dd-MM-yyyy HH:mm"),
                Description = request.Description ??
                    "Payment transfer"
            };
            await _paymentRepo.AddPayment(payment);

            // Update sender balance (Debit)
            decimal senderNewBalance =
                senderWallet.Balance - request.Amount;
            await _walletRepo.UpdateWalletBalance(
                request.SenderWalletID, senderNewBalance);

            // Record debit transaction for sender
            await _txRepo.AddTransaction(new Transaction
            {
                WalletID = request.SenderWalletID,
                Type = "Debit",
                Amount = request.Amount,
                Description = "Payment sent to Wallet #" +
                    request.ReceiverWalletID +
                    (request.Description != null ?
                    " - " + request.Description : ""),
                Date = DateTime.Now
                    .ToString("dd-MM-yyyy HH:mm"),
                Status = "Completed",
                FundingMethodID = 0,
                BalanceBefore = senderWallet.Balance,
                BalanceAfter = senderNewBalance
            });

            // Update receiver balance (Credit)
            decimal receiverNewBalance =
                receiverWallet.Balance + request.Amount;
            await _walletRepo.UpdateWalletBalance(
                request.ReceiverWalletID, receiverNewBalance);

            // Record credit transaction for receiver
            await _txRepo.AddTransaction(new Transaction
            {
                WalletID = request.ReceiverWalletID,
                Type = "Credit",
                Amount = request.Amount,
                Description = "Payment received from Wallet #" +
                    request.SenderWalletID +
                    (request.Description != null ?
                    " - " + request.Description : ""),
                Date = DateTime.Now
                    .ToString("dd-MM-yyyy HH:mm"),
                Status = "Completed",
                FundingMethodID = 0,
                BalanceBefore = receiverWallet.Balance,
                BalanceAfter = receiverNewBalance
            });

            _logger.LogInformation(
                "Payment sent from Wallet {Sender} to " +
                "Wallet {Receiver} Amount {Amount}",
                request.SenderWalletID,
                request.ReceiverWalletID,
                request.Amount);

            return Ok(new
            {
                message = "Payment sent successfully",
                senderNewBalance = senderNewBalance,
                amount = request.Amount,
                receiverWalletID = request.ReceiverWalletID
            });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetPaymentHistory()
        {
            var userID = await GetCurrentUserID();
            if (userID == 0) return Unauthorized();

            var wallets = await _walletRepo
                .GetWalletsByUser(userID);
            var walletIds = wallets
                .Select(w => w.WalletID).ToList();

            var allPayments = await _paymentRepo
                .GetAllPayments();
            var userPayments = allPayments
                .Where(p =>
                    walletIds.Contains(p.SenderWalletID) ||
                    walletIds.Contains(p.ReceiverWalletID))
                .OrderByDescending(p => p.AuthorizedDate)
                .ToList();

            return Ok(userPayments);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllPayments()
        {
            var payments = await _paymentRepo
                .GetAllPayments();
            return Ok(payments);
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

    public class SendPaymentRequest
    {
        public int SenderWalletID { get; set; }
        public int ReceiverWalletID { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }
}

