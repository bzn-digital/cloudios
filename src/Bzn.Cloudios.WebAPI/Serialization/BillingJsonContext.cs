using System.Text.Json.Serialization;
using Bzn.Cloudios.Domain.Dto;

namespace Bzn.Cloudios.WebAPI.Serialization;

[JsonSerializable(typeof(RealmBillingResponse))]
[JsonSerializable(typeof(BillingServiceItem))]
[JsonSerializable(typeof(GlobalBillingResponse))]
[JsonSerializable(typeof(RealmBillingItem))]
public sealed partial class BillingJsonContext : JsonSerializerContext;
