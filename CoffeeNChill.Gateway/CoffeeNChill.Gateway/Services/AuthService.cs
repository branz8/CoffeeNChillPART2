using Azure.Data.Tables;
using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CoffeeNChill.Gateway.Services
{
    public class AuthService
    {
        private readonly TableClient _tableClient;
        private readonly IConfiguration _config;

        public AuthService(IConfiguration config)
        {
            _config = config;
            var conn = _config["AzureWebJobsStorage"] ?? "UseDevelopmentStorage=true";
            var serviceClient = new TableServiceClient(conn);
            _tableClient = serviceClient.GetTableClient("Users");
            _tableClient.CreateIfNotExists();
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var existing = await _tableClient.GetEntityAsync<UserEntity>(request.Role, request.Email);
                if (existing != null) return false; // User exists in this role
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                // User doesn't exist, proceed to create
            }

            var user = new UserEntity
            {
                PartitionKey = request.Role,
                RowKey = request.Email,
                FullName = request.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            await _tableClient.AddEntityAsync(user);
            return true;
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            // We don't know the role yet, so we must query across partitions
            var query = _tableClient.QueryAsync<UserEntity>(u => u.RowKey == request.Email);

            await foreach (var user in query)
            {
                if (BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    var token = GenerateJwtToken(user);
                    return new AuthResponse
                    {
                        Token = token,
                        Role = user.PartitionKey,
                        FullName = user.FullName
                    };
                }
            }
            return null;
        }

        private string GenerateJwtToken(UserEntity user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.RowKey),
                new Claim(ClaimTypes.Role, user.PartitionKey)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}