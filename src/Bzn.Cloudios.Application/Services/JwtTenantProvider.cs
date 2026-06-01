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
            RealmId = Guid.Empty;
            Role = string.Empty;
            UserId = Guid.Empty;
            return;
        }

        var realmIdClaim = user.FindFirst("RealmId")?.Value;
        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("Role")?.Value ?? string.Empty;
        var userIdClaim = user.FindFirst("UserId")?.Value;

        RealmId = Guid.TryParse(realmIdClaim, out var realmId) ? realmId : Guid.Empty;
        Role = roleClaim;
        UserId = Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
