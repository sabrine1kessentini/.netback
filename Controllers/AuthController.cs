using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MonRestoAPI.Data;
using MonRestoAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MonRestoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public AuthController(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        // Action pour l'inscription
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegister userRegister)
        {
            // Vérifier si le nom d'utilisateur ou l'email existe déjà
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == userRegister.Username || u.Email == userRegister.Email);
            if (existingUser != null)
            {
                return BadRequest("Username or email already exists.");
            }

            // Générer un hachage du mot de passe
            var passwordHash = ComputePasswordHash(userRegister.Password);

            // Créer un nouvel utilisateur
            var newUser = new User
            {
                Username = userRegister.Username,
                Email = userRegister.Email,
                PasswordHash = passwordHash
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully.");
        }

        // Action pour la connexion
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLogin userLogin)
        {
            // Rechercher l'utilisateur dans la base de données
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == userLogin.Username);

            if (user == null)
            {
                return Unauthorized("Invalid username or password.");
            }

            // Vérifier le mot de passe
            var computedHash = ComputePasswordHash(userLogin.Password);

            if (user.PasswordHash != computedHash)
            {
                return Unauthorized("Invalid username or password.");
            }

            // Générer le token JWT
            var token = GenerateJwtToken(user.Username);
            return Ok(new { Token = token });
        }

        private string GenerateJwtToken(string username)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(int.Parse(_configuration["Jwt:ExpireMinutes"])),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string ComputePasswordHash(string password)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["PasswordHashKey"]); // Clé partagée pour le hachage
            using var hmac = new System.Security.Cryptography.HMACSHA256(key);
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }
    }

    public class UserLogin
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class UserRegister
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }

}