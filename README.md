# 💳 PayWallet — Digital Payment Wallet

A full-stack digital payment wallet application (PayPal/Paytm-style) built with **ASP.NET Core Web API (.NET 10)** and a clean **3-layer SOLID architecture**.

🌐 **Live Demo:** [https://rk20597.github.io/PaymentWallet](https://rk20597.github.io/PaymentWallet)

---

## ✨ Features

| Feature | Details |
|---|---|
| 🔐 Auth | Register, login with JWT Bearer tokens (8hr expiry), BCrypt password hashing |
| 👤 Accounts | Create and manage payment accounts |
| 💰 Wallets | Multi-currency wallets linked to accounts (duplicate currency prevention) |
| 💳 Funding Methods | Add Card / Bank / UPI with automatic data masking |
| ➕ Add Money | Top up wallet balance via funding method (currency validation) |
| 💸 Send Payment | Inter-wallet payments with Hyperswitch sandbox integration |
| 📋 Transactions | Full history with Balance Before/After, filter, search, sort |
| 🔄 Refunds | User requests refund → Admin approves → Balances reversed |
| ❌ Cancel Payment | Cancel authorized payment with automatic balance reversal |
| 📧 Email Notifications | Payment sent/received, money added, refund notifications |
| 🛡️ Admin Panel | Manage all users, accounts, wallets, payments, approve refunds |
| 📊 Reconciliation | Admin transaction reconciliation report |

---

## 🏗️ Architecture

```
PaymentWallet/
├── Backend/PaymentWallet.API/
│   ├── PaymentWallet.API/          ← Controllers, Services, Program.cs
│   │   ├── Controllers/            ← Auth, Account, Wallet, FundingMethod, Payment
│   │   ├── Services/               ← HyperswitchService, EmailService
│   │   └── wwwroot/                ← Frontend files (index.html, dashboard.html)
│   ├── PaymentWallet.Core/         ← Models + Interfaces (no implementation)
│   │   ├── Models/                 ← 9 models
│   │   └── Interfaces/             ← IUserRepo, IWalletRepo, IEmailService, etc.
│   └── PaymentWallet.Infrastructure/  ← Repositories + ExcelContext
│       ├── Data/                   ← ExcelContext with SemaphoreSlim(1,1)
│       └── Repositories/           ← 6 concrete repositories
├── docs/                           ← GitHub Pages (index.html, dashboard.html)
└── Data/PaymentWallet.xlsx         ← Excel data store (8 sheets)
```

### 3-Layer Flow

```
Frontend (GitHub Pages)
       ↓ HTTPS + JWT Bearer
API Gateway (ASP.NET Core)
       ↓
Controllers (Auth / Account / Wallet / FundingMethod / Payment)
       ↓
Services (HyperswitchService / EmailService)
       ↓
Repositories via interfaces (IUserRepository, IWalletRepository, ...)
       ↓
ExcelContext + SemaphoreSlim(1,1) lock
       ↓
PaymentWallet.xlsx
```

---

## 🎯 SOLID Principles

| Principle | Implementation |
|---|---|
| **S** — Single Responsibility | Each repository handles exactly one Excel sheet |
| **O** — Open/Closed | Interfaces allow new DB implementations without changing controllers |
| **L** — Liskov Substitution | Any repo implementing IUserRepository is interchangeable |
| **I** — Interface Segregation | Controllers inject only the interfaces they need |
| **D** — Dependency Inversion | Controllers depend on `IWalletRepository`, not `WalletRepository` |

---

## 🛠️ Tech Stack

- **Backend:** ASP.NET Core Web API (.NET 10)
- **Frontend:** HTML5 + CSS3 + Vanilla JavaScript (SPA)
- **Data Store:** Excel (.xlsx) via EPPlus
- **Auth:** JWT Bearer + BCrypt.Net-Next
- **Payment Gateway:** Hyperswitch sandbox (Stripe Dummy connector)
- **Email:** MailKit (SMTP)
- **Docs:** Swagger UI
- **Hosting:** GitHub Pages

---

## 📊 Excel Schema

| Sheet | Key Columns |
|---|---|
| Users | UserID, UserName, PasswordHash, Role, FullName |
| Accounts | AccountID, UserID (FK), AccountName, Status |
| Wallet | WalletID, AccountID (FK), Balance, Currency |
| Transaction | TransactionID, Type, Amount, BalanceBefore, BalanceAfter |
| FundingMethods | Type (Card/Bank/UPI), MaskedDetails |
| Payments | SenderWalletID, ReceiverWalletID, HyperswitchPaymentID, Status |
| Refunds | PaymentID (FK), Amount, Reason, Status |
| Notifications | UserID (FK), Type, Message, IsRead |

---

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK
- Node.js (optional, for frontend tooling)

### Run Locally

```bash
# Clone the repo
git clone https://github.com/rk20597/PaymentWallet.git

# Navigate to API project
cd PaymentWallet/Backend/PaymentWallet.API/PaymentWallet.API

# Run
dotnet run
```

API runs on `http://localhost:5235` | Swagger UI at `http://localhost:5235/swagger`

### Test Credentials

| Role | Email | Password |
|---|---|---|
| Admin | Admin1@paywallet.com | Admin@123 |
| User | john@gmail.com | Admin@123 |

---

## 🔐 Security

- Passwords hashed with **BCrypt** (never stored in plain text)
- **JWT tokens** with 8hr expiry and HS256 signing
- **Role-based authorization** — Admin endpoints protected separately
- **Rate limiting** — 100 req/min general, 10 req/min on auth
- **Card/Bank masking** before storage
- **SemaphoreSlim(1,1)** prevents concurrent Excel file corruption
- **Currency validation** on all wallet operations

---

## 📱 Frontend Tabs

1. **Login / Register** — Auth with JWT
2. **Overview** — Stats, wallet cards, accounts
3. **Accounts** — Create and view accounts
4. **Wallets** — Multi-currency wallet management
5. **Funding Methods** — Card / Bank / UPI
6. **Add Money** — Top up with currency validation
7. **Send Money** — Inter-wallet payments + history + refund requests
8. **Transactions** — Full history with filters and Balance Before/After
9. **Admin Panel** — Full system management + refund approvals

---

## 📈 Send Payment Flow

```
User selects sender wallet + receiver wallet ID + amount
        ↓
Validate ownership, balance, currency match
        ↓
Create payment in Hyperswitch (authorize)
        ↓
Confirm + Capture via Hyperswitch API
        ↓
Record Debit on sender (BalanceBefore/After)
Record Credit on receiver (BalanceBefore/After)
        ↓
Update both wallet balances in Excel
        ↓
Email notifications → sender + receiver
```

---

## 🔄 Refund Flow

```
User: Request Refund on completed payment
        ↓
Status → RefundRequested
        ↓
Admin: Approve Refund in Admin Panel
        ↓
Balances reversed (sender credited, receiver debited)
New transactions recorded
        ↓
Status → Refunded + Email notifications
```

---

## 📧 Email Notifications

Triggered on:
- 💸 Payment sent (sender notification)
- 💰 Payment received (receiver notification)
- ➕ Money added to wallet
- 🔄 Refund approved

---

## 🗓️ Project Timeline

| Week | Delivered |
|---|---|
| Week 5 | Schema, .NET solution, auth (BCrypt + JWT), accounts, wallets |
| Week 6 | Funding methods, add money, transactions with filters |
| Week 7 | SOLID refactor, send/receive payments, Hyperswitch, GitHub Pages |
| Week 8 | Email notifications, refund flow, cancel payment, admin panel, reconciliation |

---

*IAP Project 2026 — Rohan Kulkarni — Deloitte USI*
