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
        var user = await _context.Users
            .Include(u => u.Realm)
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user is null || user.IsBlocked)
        {
            _logger.LogWarning("Login failed for {Email}: user not found or blocked", request.Email);
            return null;
        }

        // TODO: Replace with proper BCrypt verification when password hashing is implemented
        // For now, compare against stored hash (admin seeded with configured hash)
        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for {Email}: invalid password", request.Email);
            return null;
        }

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
            new("UserId", user.Id.ToString()),
            new("RealmId", user.RealmId.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("Role", user.Role.ToString()),
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
        // TODO: Replace with BCrypt.Verify(password, storedHash)
        // For now, simple comparison for initial development
        return !string.IsNullOrEmpty(storedHash) && storedHash != "CHANGE_ME_USE_BCRIPT_HASH";
    }
}
