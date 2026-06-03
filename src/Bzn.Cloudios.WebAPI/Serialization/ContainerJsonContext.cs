using System.Text.Json.Serialization;
using Bzn.Cloudios.Domain.Dto;

namespace Bzn.Cloudios.WebAPI.Serialization;

[JsonSerializable(typeof(ContainerLogsResponse))]
[JsonSerializable(typeof(ContainerLogEntry))]
[JsonSerializable(typeof(ContainerVolumeRequest))]
public sealed partial class ContainerJsonContext : JsonSerializerContext;
