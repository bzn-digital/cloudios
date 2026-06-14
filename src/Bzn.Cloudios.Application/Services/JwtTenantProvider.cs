using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Bzn.Cloudios.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Bzn.Cloudios.Application.Services;

public sealed class JwtTenantProvider : ITenantProvider
{
    public Guid RealmId { get; }
    public string Role { get; }
    public Guid UserId { get; }

    public JwtTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            // Unauthenticated context — grant no privileges.
            // Protected endpoints are blocked by the FallbackPolicy; this only
            // runs for AllowAnonymous endpoints that happen to resolve ITenantProvider.
            RealmId = Guid.Empty;
            Role = string.Empty;
            UserId = Guid.Empty;
            return;
        }

        var realmIdClaim = user.FindFirst("realmId")?.Value;
        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value ?? string.Empty;
        var userIdClaim = user.FindFirst("sub")?.Value;

        RealmId = Guid.TryParse(realmIdClaim, out var realmId) ? realmId : Guid.Parse("00000000-0000-0000-0000-000000000001");
        Role = roleClaim;
        UserId = Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
