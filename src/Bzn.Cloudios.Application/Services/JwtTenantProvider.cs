using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Bzn.Cloudios.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Bzn.Cloudios.Application.Services;

public sealed class JwtTenantProvider : ITenantProvider
{
    public Guid RealmId { get; }
    public string Role { get; }
    public Guid UserId { get; }

    public JwtTenantProvider(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            // Fallback to configured default realm for testing when auth is disabled
            var defaultRealmId = configuration["DefaultRealmId"];
            RealmId = Guid.TryParse(defaultRealmId, out var defaultParsedRealmId) ? defaultParsedRealmId : Guid.Parse("00000000-0000-0000-0000-000000000001");
            Role = "PlatformAdmin";
            UserId = Guid.Empty;
            return;
        }

        var realmIdClaim = user.FindFirst("realmId")?.Value;
        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value ?? string.Empty;
        var userIdClaim = user.FindFirst("sub")?.Value;

        RealmId = Guid.TryParse(realmIdClaim, out var claimParsedRealmId) ? claimParsedRealmId : Guid.Parse("00000000-0000-0000-0000-000000000001");
        Role = roleClaim;
        UserId = Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
