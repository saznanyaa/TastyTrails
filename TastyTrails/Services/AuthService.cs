using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TastyTrails.Models;
using TastyTrails.Models.DTOs;

namespace TastyTrails.Services
{
    public class AuthService
    {
        private readonly PasswordHasher<MongoUser> _passwordHasher;
        private readonly IConfiguration _config;
        private readonly IMongoCollection<MongoUser> _users;

        public AuthService(IConfiguration config)
        {
            _config = config;
            _passwordHasher = new PasswordHasher<MongoUser>();

            var mongoSettings = config.GetSection("MongoSettings");
            var connectionString = mongoSettings["ConnectionString"]!;
            var databaseName = mongoSettings["DatabaseName"]!;

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _users = database.GetCollection<MongoUser>("users");
        }

        
        public async Task<object> Login(LoginDto dto)
        {
            var user = await _users 
                .Find(u => u.Email == dto.Email)
                .FirstOrDefaultAsync();

            if (user == null)
                throw new Exception("Invalid credentials");

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

            if (result == PasswordVerificationResult.Failed)
                throw new Exception("Invalid credentials");

            var token = GenerateToken(user);

            return new
            {
                Token = token,
                UserId = user.Id.ToString()
            };
        }

        private string GenerateToken(MongoUser user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!)
            );

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("username", user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(jwtSettings["ExpiryMinutes"]!)
                ),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateTokenForUser(MongoUser user)
        {
            return GenerateToken(user);
        }
    }
}