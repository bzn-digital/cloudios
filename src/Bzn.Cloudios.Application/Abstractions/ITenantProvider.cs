namespace Bzn.Cloudios.Application.Abstractions;

public interface ITenantProvider
{
    Guid RealmId { get; }
    string Role { get; }
    Guid UserId { get; }
}
