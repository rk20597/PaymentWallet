using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PaymentWallet.API.Repositories;
using PaymentWallet.Core.Interfaces;
using PaymentWallet.Core.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PaymentWallet.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserRepository userRepo,
            IConfiguration config,
            ILogger<AuthController> logger)
        {
            _userRepo = userRepo;
            _config = config;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request)
        {
            _logger.LogInformation("Attempting to register new user.");
            if (string.IsNullOrEmpty(request.UserName) ||
                string.IsNullOrEmpty(request.Password))
                return BadRequest(new
                {
                    message = "Username and password required"
                });

            var existing = await _userRepo
                .GetUserByUsername(request.UserName);
            if (existing != null)
                return BadRequest(new
                {
                    message = "Username already exists"
                });

            var user = new Users
            {
                UserName = request.UserName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role ?? "User",
                IsActive = true,
                FullName = request.FullName,
                Phone = request.Phone,
                CreatedDate = DateTime.Now
                    .ToString("dd-MM-yyyy")
            };

            await _userRepo.AddUser(user);

            return Ok(new
            {
                message = "Registration successful"
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            _logger.LogInformation("Attempting to log in user {UserName}", request.UserName);
            if (string.IsNullOrEmpty(request.UserName) ||
                string.IsNullOrEmpty(request.Password))
                return BadRequest(new
                {
                    message = "Username and password required"
                });

            var user = await _userRepo
                .GetUserByUsername(request.UserName);

            if (user == null ||
                !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
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

        [HttpGet("allusers")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userRepo.GetAllUsers();
            return Ok(users);
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

