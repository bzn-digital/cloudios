using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Bzn.Cloudios.Application.Services;

public sealed class AuthService
{
    private readonly CloudiosDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(CloudiosDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Login attempt for {Email} in realm {RealmName}", request.Email, request.RealmName);

        var user = await _context.Users
            .Include(u => u.Realm)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.Realm.Name == request.RealmName, ct);

        if (user is null)
        {
            _logger.LogWarning("Login failed for {Email}: user not found in realm {RealmName}", request.Email, request.RealmName);
            
            // Log all users for debugging
            var allUsers = await _context.Users.Include(u => u.Realm).ToListAsync(ct);
            _logger.LogInformation("Total users in database: {Count}", allUsers.Count);
            foreach (var u in allUsers)
            {
                _logger.LogInformation("User: {Email}, Realm: {RealmName}, Role: {Role}, Blocked: {Blocked}", 
                    u.Email, u.Realm?.Name, u.Role, u.IsBlocked);
            }
            
            return null;
        }

        if (user.IsBlocked)
        {
            _logger.LogWarning("Login failed for {Email}: user is blocked", request.Email);
            return null;
        }

        _logger.LogInformation("User found: {Email}, verifying password", request.Email);

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for {Email}: invalid password", request.Email);
            return null;
        }

        _logger.LogInformation("Login successful for {Email}", request.Email);

        var token = GenerateJwt(user);
        var expiresAt = DateTime.UtcNow.AddHours(8);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role.ToString(),
                RealmId = user.RealmId,
                RealmName = user.Realm.Name
            }
        };
    }

    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("realmId", user.RealmId.ToString()),
            new("realmName", user.Realm.Name),
            new("role", user.Role.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var issuer = _configuration["Jwt:Issuer"] ?? "cloudios";
        var audience = _configuration["Jwt:Audience"] ?? "cloudios-api";

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, storedHash);
    }
}
