namespace Bzn.Cloudios.Application.Events;

public sealed record ContainerStartedEvent(Guid ContainerId, Guid RealmId, string ContainerName, DateTime OccurredAt);
public sealed record ContainerStoppedEvent(Guid ContainerId, Guid RealmId, string ContainerName, DateTime OccurredAt);
public sealed record ContainerDeletedEvent(Guid ContainerId, Guid RealmId, string ContainerName, DateTime OccurredAt);
public sealed record ContainerFailedEvent(Guid ContainerId, Guid RealmId, string ContainerName, string ErrorMessage, DateTime OccurredAt);
public sealed record RealmBlockedEvent(Guid RealmId, string RealmName, bool IsBlocked, DateTime OccurredAt);
