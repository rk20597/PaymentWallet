using PaymentWallet.API.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OfficeOpenXml;

ExcelPackage.License.SetNonCommercialPersonal(
    "PaymentWallet");

var builder = WebApplication.CreateBuilder(args);

var dataPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "..", "..", "Data", "PaymentWallet.xlsx");

Console.WriteLine($"Data path: {dataPath}");
Console.WriteLine($"File exists: {File.Exists(dataPath)}");

builder.Services.AddSingleton<PaymentWalletRepository>(
    new PaymentWalletRepository(dataPath));

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

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();
