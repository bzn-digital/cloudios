namespace Bzn.Cloudios.Domain.Entities;

public sealed class Realm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public List<User> Users { get; set; } = [];
    public List<Container> Containers { get; set; } = [];
}
