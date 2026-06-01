using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Microsoft.AspNetCore.Authorization;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class BillingEndpoints
{
    public static void MapBillingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/billing");

        group.MapGet("/realm", async (ITenantProvider tenant, BillingService billing, CancellationToken ct) =>
        {
            var now = DateTime.UtcNow;
            var cost = await billing.GetRealmBillingAsync(tenant.RealmId, now.Year, now.Month, ct);
            return Results.Ok(new { Month = $"{now.Year:0000}-{now.Month:00}", TotalCostBRL = cost });
        }).RequireAuthorization("RequireRealmMember");

        group.MapGet("/global", async (BillingService billing, int? year, int? month, CancellationToken ct) =>
        {
            var now = DateTime.UtcNow;
            var targetYear = year ?? now.Year;
            var targetMonth = month ?? now.Month;
            var cost = await billing.GetGlobalBillingAsync(targetYear, targetMonth, ct);
            return Results.Ok(new { Month = $"{targetYear:0000}-{targetMonth:00}", TotalRevenueBRL = cost });
        }).RequireAuthorization("RequirePlatformAdmin");
    }
}
