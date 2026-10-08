using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentWallet.API.Services;
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
        private readonly IHyperswitchService _hyperswitch;
        private readonly IEmailService _emailService;

        public PaymentController(
            IPaymentRepository paymentRepo,
            IWalletRepository walletRepo,
            IUserRepository userRepo,
            ITransactionRepository txRepo,
            ILogger<PaymentController> logger,
            IHyperswitchService hyperswitch,
            IEmailService emailService)
        {
            _paymentRepo = paymentRepo;
            _walletRepo = walletRepo;
            _userRepo = userRepo;
            _txRepo = txRepo;
            _logger = logger;
            _hyperswitch = hyperswitch;
            _emailService = emailService;
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
            // Create payment via Hyperswitch
            var hsResponse = await _hyperswitch.CreatePayment(
                request.Amount,
                senderWallet.Currency ?? "INR",
                request.Description ?? "Payment transfer");

            string hyperswitchId = hsResponse.PaymentId ??
                "LOCAL_" + Guid.NewGuid().ToString("N")[..8];
            string paymentStatus = "Authorized";

            // Confirm payment
            if (hsResponse.PaymentId != null)
            {
                var confirmResponse = await _hyperswitch
                    .ConfirmPayment(hsResponse.PaymentId);

                if (confirmResponse.Status == "requires_capture")
                {
                    // Capture payment
                    var captureResponse = await _hyperswitch
                        .CapturePayment(
                            hsResponse.PaymentId,
                            request.Amount);
                    paymentStatus = captureResponse.Status ==
                        "succeeded" ? "Completed" : "Failed";
                }
                else if (confirmResponse.Status == "succeeded")
                {
                    paymentStatus = "Completed";
                }
                else
                {
                    paymentStatus = "Failed";
                }
            }

            // If payment failed don't proceed
            if (paymentStatus == "Failed")
                return BadRequest(new
                {
                    message = "Payment processing failed. " +
                        "Please try again."
                });

            // Create payment record
            var payment = new Payments
            {
                SenderWalletID = request.SenderWalletID,
                ReceiverWalletID = request.ReceiverWalletID,
                Amount = request.Amount,
                Status = paymentStatus,
                HyperSwitchPaymentID = hyperswitchId,
                AuthorizedDate = DateTime.Now
                    .ToString("dd-MM-yyyy HH:mm"),
                CapturedDate = paymentStatus == "Completed" ?
                    DateTime.Now.ToString("dd-MM-yyyy HH:mm") : null,
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

            var currentUser = (await _userRepo.GetAllUsers()).FirstOrDefault(u => u.UserID == userID);
            if (currentUser?.UserName != null)
            {
                await _emailService.SendPaymentSentEmail(
                    currentUser.UserName,
                    request.Amount,
                    senderWallet.Currency ?? "INR",
                    request.ReceiverWalletID);
            }
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

        [HttpPost("{paymentId}/cancel")]
        public async Task<IActionResult> CancelPayment(
    int paymentId)
        {
            var userID = await GetCurrentUserID();
            if (userID == 0) return Unauthorized();

            var payments = await _paymentRepo.GetAllPayments();
            var payment = payments.FirstOrDefault(p =>
                p.PaymentID == paymentId);
            if (payment == null)
                return NotFound(new
                {
                    message = "Payment not found"
                });

            // Verify payment belongs to user
            var wallets = await _walletRepo
                .GetWalletsByUser(userID);
            var walletIds = wallets
                .Select(w => w.WalletID).ToList();
            if (!walletIds.Contains(payment.SenderWalletID))
                return Unauthorized(new
                {
                    message = "Not authorized"
                });

            if (payment.Status == "Completed")
                return BadRequest(new
                {
                    message = "Cannot cancel completed payment. Use refund."
                });

            // Update payment status
            await _paymentRepo.UpdatePaymentStatus(
                paymentId, "Cancelled");

            // Reverse wallet balances
            var allWallets = await _walletRepo.GetAllWallets();
            var senderWallet = allWallets.FirstOrDefault(w =>
                w.WalletID == payment.SenderWalletID);
            var receiverWallet = allWallets.FirstOrDefault(w =>
                w.WalletID == payment.ReceiverWalletID);

            if (senderWallet != null && receiverWallet != null)
            {
                // Reverse debit on sender
                await _walletRepo.UpdateWalletBalance(
                    payment.SenderWalletID,
                    senderWallet.Balance + payment.Amount);

                // Reverse credit on receiver
                await _walletRepo.UpdateWalletBalance(
                    payment.ReceiverWalletID,
                    receiverWallet.Balance - payment.Amount);

                // Record reversal transactions
                await _txRepo.AddTransaction(new Transaction
                {
                    WalletID = payment.SenderWalletID,
                    Type = "Credit",
                    Amount = payment.Amount,
                    Description = "Payment cancelled - refund",
                    Date = DateTime.Now
                        .ToString("dd-MM-yyyy HH:mm"),
                    Status = "Completed",
                    FundingMethodID = 0,
                    BalanceBefore = senderWallet.Balance,
                    BalanceAfter = senderWallet.Balance +
                        payment.Amount
                });

                await _txRepo.AddTransaction(new Transaction
                {
                    WalletID = payment.ReceiverWalletID,
                    Type = "Debit",
                    Amount = payment.Amount,
                    Description = "Payment cancelled - reversal",
                    Date = DateTime.Now
                        .ToString("dd-MM-yyyy HH:mm"),
                    Status = "Completed",
                    FundingMethodID = 0,
                    BalanceBefore = receiverWallet.Balance,
                    BalanceAfter = receiverWallet.Balance -
                        payment.Amount
                });
            }

            _logger.LogInformation(
                "Payment {PaymentId} cancelled", paymentId);

            return Ok(new
            {
                message = "Payment cancelled successfully"
            });
        }

        [HttpPost("{paymentId}/refund")]
        public async Task<IActionResult> RefundPayment(
            int paymentId,
            [FromBody] RefundRequest request)
        {
            var userID = await GetCurrentUserID();
            if (userID == 0) return Unauthorized();

            var payments = await _paymentRepo.GetAllPayments();
            var payment = payments.FirstOrDefault(p =>
                p.PaymentID == paymentId);
            if (payment == null)
                return NotFound(new
                {
                    message = "Payment not found"
                });

            if (payment.Status != "Completed")
                return BadRequest(new
                {
                    message = "Only completed payments can be refunded"
                });

            if (request.Amount > payment.Amount)
                return BadRequest(new
                {
                    message = "Refund amount cannot exceed payment amount"
                });

            // Update payment status
            await _paymentRepo.UpdatePaymentStatus(
                paymentId, "Refunded");

            // Reverse wallet balances
            var allWallets = await _walletRepo.GetAllWallets();
            var senderWallet = allWallets.FirstOrDefault(w =>
                w.WalletID == payment.SenderWalletID);
            var receiverWallet = allWallets.FirstOrDefault(w =>
                w.WalletID == payment.ReceiverWalletID);

            if (senderWallet != null && receiverWallet != null)
            {
                decimal refundAmount = request.Amount > 0 ?
                    request.Amount : payment.Amount;

                // Refund to sender
                await _walletRepo.UpdateWalletBalance(
                    payment.SenderWalletID,
                    senderWallet.Balance + refundAmount);

                // Deduct from receiver
                await _walletRepo.UpdateWalletBalance(
                    payment.ReceiverWalletID,
                    receiverWallet.Balance - refundAmount);

                // Record refund transactions
                await _txRepo.AddTransaction(new Transaction
                {
                    WalletID = payment.SenderWalletID,
                    Type = "Credit",
                    Amount = refundAmount,
                    Description = "Refund received - " +
                        (request.Reason ?? "Payment refund"),
                    Date = DateTime.Now
                        .ToString("dd-MM-yyyy HH:mm"),
                    Status = "Completed",
                    FundingMethodID = 0,
                    BalanceBefore = senderWallet.Balance,
                    BalanceAfter = senderWallet.Balance +
                        refundAmount
                });

                await _txRepo.AddTransaction(new Transaction
                {
                    WalletID = payment.ReceiverWalletID,
                    Type = "Debit",
                    Amount = refundAmount,
                    Description = "Refund issued - " +
                        (request.Reason ?? "Payment refund"),
                    Date = DateTime.Now
                        .ToString("dd-MM-yyyy HH:mm"),
                    Status = "Completed",
                    FundingMethodID = 0,
                    BalanceBefore = receiverWallet.Balance,
                    BalanceAfter = receiverWallet.Balance -
                        refundAmount
                });
            }

            _logger.LogInformation(
                "Payment {PaymentId} refunded", paymentId);

            var currentUser = (await _userRepo.GetAllUsers()).FirstOrDefault(u => u.UserID == userID);
            if (currentUser?.UserName != null)
            {
                await _emailService.SendRefundEmail(
                    currentUser.UserName,
                    request.Amount > 0 ?
                        request.Amount : payment.Amount,
                    "INR");
            }


            return Ok(new
            {
                message = "Refund processed successfully"
            });
        }

        [HttpPost("{paymentId}/requestrefund")]
        public async Task<IActionResult> RequestRefund(
    int paymentId,
    [FromBody] RefundRequest request)
        {
            var userID = await GetCurrentUserID();
            if (userID == 0) return Unauthorized();

            var payments = await _paymentRepo.GetAllPayments();
            var payment = payments.FirstOrDefault(p =>
                p.PaymentID == paymentId);
            if (payment == null)
                return NotFound(new
                {
                    message = "Payment not found"
                });

            if (payment.Status != "Completed")
                return BadRequest(new
                {
                    message = "Only completed payments can be refunded"
                });

            // Verify payment belongs to user
            var wallets = await _walletRepo
                .GetWalletsByUser(userID);
            var walletIds = wallets
                .Select(w => w.WalletID).ToList();
            if (!walletIds.Contains(payment.SenderWalletID))
                return Unauthorized(new
                {
                    message = "Not authorized"
                });

            await _paymentRepo.UpdatePaymentStatus(
                paymentId, "RefundRequested");

            _logger.LogInformation(
                "Refund requested for Payment {PaymentId}",
                paymentId);

            return Ok(new
            {
                message = "Refund request submitted. " +
                    "Pending admin approval."
            });
        }

        [HttpPost("{paymentId}/approverefund")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveRefund(
            int paymentId)
        {
            var payments = await _paymentRepo.GetAllPayments();
            var payment = payments.FirstOrDefault(p =>
                p.PaymentID == paymentId);
            if (payment == null)
                return NotFound(new
                {
                    message = "Payment not found"
                });

            if (payment.Status != "RefundRequested")
                return BadRequest(new
                {
                    message = "Payment is not pending refund"
                });

            // Process actual refund
            await _paymentRepo.UpdatePaymentStatus(
                paymentId, "Refunded");

            var allWallets = await _walletRepo.GetAllWallets();
            var senderWallet = allWallets.FirstOrDefault(w =>
                w.WalletID == payment.SenderWalletID);
            var receiverWallet = allWallets.FirstOrDefault(w =>
                w.WalletID == payment.ReceiverWalletID);

            if (senderWallet != null && receiverWallet != null)
            {
                await _walletRepo.UpdateWalletBalance(
                    payment.SenderWalletID,
                    senderWallet.Balance + payment.Amount);

                await _walletRepo.UpdateWalletBalance(
                    payment.ReceiverWalletID,
                    receiverWallet.Balance - payment.Amount);

                await _txRepo.AddTransaction(new Transaction
                {
                    WalletID = payment.SenderWalletID,
                    Type = "Credit",
                    Amount = payment.Amount,
                    Description = "Refund approved by Admin",
                    Date = DateTime.Now
                        .ToString("dd-MM-yyyy HH:mm"),
                    Status = "Completed",
                    FundingMethodID = 0,
                    BalanceBefore = senderWallet.Balance,
                    BalanceAfter = senderWallet.Balance +
                        payment.Amount
                });

                await _txRepo.AddTransaction(new Transaction
                {
                    WalletID = payment.ReceiverWalletID,
                    Type = "Debit",
                    Amount = payment.Amount,
                    Description = "Refund deducted - Admin approved",
                    Date = DateTime.Now
                        .ToString("dd-MM-yyyy HH:mm"),
                    Status = "Completed",
                    FundingMethodID = 0,
                    BalanceBefore = receiverWallet.Balance,
                    BalanceAfter = receiverWallet.Balance -
                        payment.Amount
                });
            }

            _logger.LogInformation(
                "Refund approved for Payment {PaymentId}",
                paymentId);

            return Ok(new
            {
                message = "Refund approved successfully"
            });
        }


    }

    public class SendPaymentRequest
    {
        public int SenderWalletID { get; set; }
        public int ReceiverWalletID { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }

    public class RefundRequest
    {
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
    }

}

