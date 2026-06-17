using System.Text.Json.Serialization;
using Bzn.Cloudios.Domain.Dto;

namespace Bzn.Cloudios.WebAPI.Serialization;

[JsonSerializable(typeof(CreateManagedAppRequest))]
[JsonSerializable(typeof(ManagedAppResponse))]
[JsonSerializable(typeof(ManagedAppListResponse))]
[JsonSerializable(typeof(ManagedAppActionResponse))]
[JsonSerializable(typeof(ManagedAppTemplateResponse))]
[JsonSerializable(typeof(ManagedAppTemplateListResponse))]
[JsonSerializable(typeof(AdminManagedAppResponse))]
[JsonSerializable(typeof(AdminManagedAppListResponse))]
public sealed partial class ManagedAppJsonContext : JsonSerializerContext;
