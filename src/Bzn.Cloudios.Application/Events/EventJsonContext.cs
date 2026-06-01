using System.Text.Json.Serialization;
using Bzn.Cloudios.Application.Events;
using Bzn.Cloudios.Application.Services;

namespace Bzn.Cloudios.Application.Events;

[JsonSerializable(typeof(ContainerStartedEvent))]
[JsonSerializable(typeof(ContainerStoppedEvent))]
[JsonSerializable(typeof(ContainerDeletedEvent))]
[JsonSerializable(typeof(ContainerFailedEvent))]
[JsonSerializable(typeof(RealmBlockedEvent))]
[JsonSerializable(typeof(EventEnvelope))]
public sealed partial class EventJsonContext : JsonSerializerContext;
