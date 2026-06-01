using System.Text.Json.Serialization;
using Bzn.Cloudios.Domain.Dto;

namespace Bzn.Cloudios.WebAPI.Serialization;

[JsonSerializable(typeof(ContainerLogsResponse))]
[JsonSerializable(typeof(ContainerLogEntry))]
public sealed partial class ContainerJsonContext : JsonSerializerContext;
