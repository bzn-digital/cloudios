namespace Bzn.Cloudios.Domain.Dto;

public sealed class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Uptime { get; set; } = string.Empty;
}
