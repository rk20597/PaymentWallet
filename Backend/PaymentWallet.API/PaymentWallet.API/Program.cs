using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OfficeOpenXml;
using PaymentWallet.API.Repositories;
using PaymentWallet.API.Services;
using System.Text;
using System.Threading.RateLimiting;
using PaymentWallet.Core.Interfaces;
using PaymentWallet.Infrastructure.Repositories;
using PaymentWallet.Infrastructure.Data;

ExcelPackage.License.SetNonCommercialPersonal(
    "PaymentWallet");

var builder = WebApplication.CreateBuilder(args);

var dataPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "..", "..", "..", "Data", "PaymentWallet.xlsx");

Console.WriteLine($"Data path: {dataPath}");
Console.WriteLine($"File exists: {File.Exists(dataPath)}");

var excelContext = new ExcelContext(dataPath);

builder.Services.AddSingleton(excelContext);
builder.Services.AddSingleton<IUserRepository>(
    new UserRepository(excelContext));
builder.Services.AddSingleton<IAccountRepository>(
    new AccountRepository(excelContext));
builder.Services.AddSingleton<IWalletRepository>(
    new WalletRepository(excelContext));
builder.Services.AddSingleton<ITransactionRepository>(
    new TransactionRepository(excelContext));
builder.Services.AddSingleton<IFundingMethodRepository>(
    new FundingMethodRepository(excelContext));
builder.Services.AddSingleton<IPaymentRepository>(
    new PaymentRepository(excelContext));
builder.Services.AddSingleton<IHyperswitchService, HyperswitchService>();
builder.Services.AddSingleton<IEmailService, EmailService>();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder
                    .Configuration["Jwt:Issuer"],
                ValidAudience = builder
                    .Configuration["Jwt:Audience"],
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration[
                                "Jwt:Key"] ?? ""))
            };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder =
            QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 10;
    });

    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueProcessingOrder =
            QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 2;
    });

    options.RejectionStatusCode = 429;
});

builder.Services.AddControllers();

var app = builder.Build();
if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();
