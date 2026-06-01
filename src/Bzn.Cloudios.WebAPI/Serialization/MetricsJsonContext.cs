using System.Text.Json.Serialization;
using Bzn.Cloudios.Domain.Dto;

namespace Bzn.Cloudios.WebAPI.Serialization;

[JsonSerializable(typeof(ContainerMetricsResponse))]
[JsonSerializable(typeof(MetricDataPoint))]
[JsonSerializable(typeof(HostMetricsResponse))]
public sealed partial class MetricsJsonContext : JsonSerializerContext;
