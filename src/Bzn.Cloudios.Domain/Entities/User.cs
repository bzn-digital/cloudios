using Bzn.Cloudios.Domain.Enums;

namespace Bzn.Cloudios.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public Guid RealmId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime CreatedAt { get; set; }

    public Realm Realm { get; set; } = null!;
}
