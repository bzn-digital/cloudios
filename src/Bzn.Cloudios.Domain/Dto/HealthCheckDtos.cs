namespace Bzn.Cloudios.Domain.Dto;

public sealed class HealthCheckResponse
{
    public string Status { get; set; } = "Healthy";
    public string Version { get; set; } = string.Empty;
    public string Uptime { get; set; } = string.Empty;
    public Dictionary<string, string> Details { get; set; } = [];
}
