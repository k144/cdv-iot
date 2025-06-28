
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Database;
using Database.Models;
using System.Security.Cryptography;
using System.Text;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("User Management")]
    public class UsersController : ControllerBase
    {
        private readonly ParkingDbContext _context;
        private readonly ILogger<UsersController> _logger;
        private readonly IConfiguration _configuration;

        public UsersController(ParkingDbContext context, ILogger<UsersController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("register")]
        [SwaggerOperation(Summary = "Register a new user")]
        public async Task<ActionResult<User>> Register(string username, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                return Conflict("Username already exists");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                PasswordHash = HashPassword(password),
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login as a user")]
        public async Task<ActionResult<string>> Login(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || user.PasswordHash != HashPassword(password))
            {
                return Unauthorized("Invalid credentials");
            }

            var token = GenerateJwtToken(user);

            return Ok(token);
        }

        [HttpGet("{id}")]
        [Authorize]
        [SwaggerOperation(Summary = "Get user by ID")]
        public async Task<ActionResult<User>> GetUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
