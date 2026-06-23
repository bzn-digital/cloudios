using Bzn.Cloudios.Domain.Entities;

namespace Bzn.Cloudios.Application.Extensions;

public static class TenantQueryExtensions
{
    public static IQueryable<Container> ForRealm(this IQueryable<Container> query, Guid realmId)
        => query.Where(c => c.RealmId == realmId);

    public static IQueryable<User> ForRealm(this IQueryable<User> query, Guid realmId)
        => query.Where(u => u.RealmId == realmId);

    public static IQueryable<ContainerVolume> ForRealm(this IQueryable<ContainerVolume> query, Guid realmId)
        => query.Where(v => v.Container.RealmId == realmId);

    public static IQueryable<ContainerEnvVar> ForRealm(this IQueryable<ContainerEnvVar> query, Guid realmId)
        => query.Where(e => e.Container.RealmId == realmId);

    public static IQueryable<ManagedAppInstance> ForRealm(this IQueryable<ManagedAppInstance> query, Guid realmId)
        => query.Where(i => i.RealmId == realmId);
}
