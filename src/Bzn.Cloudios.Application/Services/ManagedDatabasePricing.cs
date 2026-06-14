using Bzn.Cloudios.Domain.Enums;

namespace Bzn.Cloudios.Application.Services;

/// <summary>
/// Deterministic billing calculator for managed databases. The hourly rate is the
/// sum of the RAM cost, the CPU cost and a fixed cost for the database engine.
/// The monthly forecast assumes a 730-hour month (the average across a year).
/// All values are expressed in Brazilian Reais (BRL).
/// </summary>
public static class ManagedDatabasePricing
{
    /// <summary>Average number of hours in a month used for the monthly forecast.</summary>
    public const decimal HoursPerMonth = 730m;

    /// <summary>Cost per vCPU core per hour.</summary>
    public const decimal CpuCoreHourBRL = 0.05m;

    /// <summary>Cost per GiB of RAM per hour.</summary>
    public const decimal MemoryGiBHourBRL = 0.02m;

    private const decimal BytesPerGiB = 1024m * 1024m * 1024m;

    /// <summary>Fixed hourly cost of running the database engine, per type.</summary>
    public static decimal EngineFixedHourBRL(ManagedDatabaseType type) => type switch
    {
        ManagedDatabaseType.MySQL => 0.10m,
        ManagedDatabaseType.MongoDB => 0.12m,
        _ => 0.10m
    };

    /// <summary>
    /// Hourly rate = RAM cost + CPU cost + fixed engine cost, rounded to 4 decimals.
    /// </summary>
    public static decimal HourlyRateBRL(double cpuLimitCores, long memoryLimitBytes, ManagedDatabaseType type)
    {
        var cpuCost = (decimal)cpuLimitCores * CpuCoreHourBRL;
        var memoryCost = (memoryLimitBytes / BytesPerGiB) * MemoryGiBHourBRL;
        var rate = cpuCost + memoryCost + EngineFixedHourBRL(type);
        return decimal.Round(rate, 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>Monthly forecast = hourly rate * 730 hours, rounded to 2 decimals.</summary>
    public static decimal MonthlyForecastBRL(decimal hourlyRateBRL) =>
        decimal.Round(hourlyRateBRL * HoursPerMonth, 2, MidpointRounding.AwayFromZero);
}
