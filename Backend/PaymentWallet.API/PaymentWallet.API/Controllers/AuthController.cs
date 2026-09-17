using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PaymentWallet.API.Models;
using PaymentWallet.API.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PaymentWallet.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly PaymentWalletRepository _repo;
        private readonly IConfiguration _config;

        public AuthController(
            PaymentWalletRepository repo,
            IConfiguration config)
        {
            _repo = repo;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.UserName) ||
                string.IsNullOrEmpty(request.Password))
                return BadRequest(new
                {
                    message = "Username and password required"
                });

            var existing = await _repo
                .GetUserByUsername(request.UserName);
            if (existing != null)
                return BadRequest(new
                {
                    message = "Username already exists"
                });

            var user = new Users
            {
                UserName = request.UserName,
                PasswordHash = request.Password,
                Role = request.Role ?? "User",
                IsActive = true,
                FullName = request.FullName,
                Phone = request.Phone,
                CreatedDate = DateTime.Now
                    .ToString("dd-MM-yyyy")
            };

            await _repo.AddUser(user);

            return Ok(new
            {
                message = "Registration successful"
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.UserName) ||
                string.IsNullOrEmpty(request.Password))
                return BadRequest(new
                {
                    message = "Username and password required"
                });

            var user = await _repo
                .GetUserByUsername(request.UserName);

            if (user == null ||
                user.PasswordHash != request.Password)
                return Unauthorized(new
                {
                    message = "Invalid credentials"
                });

            if (!user.IsActive)
                return Unauthorized(new
                {
                    message = "Account is inactive"
                });

            var token = GenerateToken(
                user.UserName!, user.Role!);

            return Ok(new
            {
                token,
                role = user.Role,
                username = user.UserName,
                fullName = user.FullName,
                userID = user.UserID
            });
        }

        private string GenerateToken(
            string username, string role)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _config["Jwt:Key"] ?? ""));
            var creds = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }

    public class RegisterRequest
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }

        public string? Role { get; set; }
    }
}

