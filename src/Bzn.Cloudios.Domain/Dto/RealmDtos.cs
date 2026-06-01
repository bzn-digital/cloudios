namespace Bzn.Cloudios.Domain.Dto;

public sealed class RealmListResponse
{
    public List<RealmItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
}

public sealed class RealmItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
    public int ContainerCount { get; set; }
}

public sealed class RealmDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RealmUserItem> Users { get; set; } = [];
}

public sealed class RealmUserItem
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
}

public sealed class CreateRealmRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateRealmRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
