using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bzn.Cloudios.Tests;

public class RealmQuotaTests
{
    private static CloudiosDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<CloudiosDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var db = new CloudiosDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public void Realm_QuotaProperties_AreNullable()
    {
        var realm = new Realm
        {
            Id = Guid.NewGuid(),
            Name = "Test Realm",
            Slug = "test-realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        Assert.Null(realm.MaxContainers);
        Assert.Null(realm.MaxDatabases);
        Assert.Null(realm.MaxManagedApps);
        Assert.Null(realm.MaxRamBytes);
        Assert.Null(realm.MaxCpuCores);
    }

    [Fact]
    public void Realm_QuotaProperties_CanBeSet()
    {
        var realm = new Realm
        {
            Id = Guid.NewGuid(),
            Name = "Test Realm",
            Slug = "test-realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            MaxContainers = 10,
            MaxDatabases = 5,
            MaxManagedApps = 20,
            MaxRamBytes = 8L * 1024 * 1024 * 1024, // 8 GB
            MaxCpuCores = 4.0
        };

        Assert.Equal(10, realm.MaxContainers);
        Assert.Equal(5, realm.MaxDatabases);
        Assert.Equal(20, realm.MaxManagedApps);
        Assert.Equal(8L * 1024 * 1024 * 1024, realm.MaxRamBytes);
        Assert.Equal(4.0, realm.MaxCpuCores);
    }

    [Fact]
    public async Task Realm_QuotaProperties_PersistToDatabase()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();

        var realm = new Realm
        {
            Id = realmId,
            Name = "Test Realm",
            Slug = "test-realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            MaxContainers = 10,
            MaxDatabases = 5,
            MaxManagedApps = 20,
            MaxRamBytes = 8L * 1024 * 1024 * 1024,
            MaxCpuCores = 4.0
        };

        db.Realms.Add(realm);
        await db.SaveChangesAsync();

        var retrieved = await db.Realms.FindAsync(realmId);
        Assert.NotNull(retrieved);
        Assert.Equal(10, retrieved!.MaxContainers);
        Assert.Equal(5, retrieved.MaxDatabases);
        Assert.Equal(20, retrieved.MaxManagedApps);
        Assert.Equal(8L * 1024 * 1024 * 1024, retrieved.MaxRamBytes);
        Assert.Equal(4.0, retrieved.MaxCpuCores);
    }

    [Fact]
    public async Task Realm_QuotaProperties_NullValuesPersistToDatabase()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();

        var realm = new Realm
        {
            Id = realmId,
            Name = "Test Realm",
            Slug = "test-realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
            // All quota properties left as null
        };

        db.Realms.Add(realm);
        await db.SaveChangesAsync();

        var retrieved = await db.Realms.FindAsync(realmId);
        Assert.NotNull(retrieved);
        Assert.Null(retrieved!.MaxContainers);
        Assert.Null(retrieved.MaxDatabases);
        Assert.Null(retrieved.MaxManagedApps);
        Assert.Null(retrieved.MaxRamBytes);
        Assert.Null(retrieved.MaxCpuCores);
    }

    [Fact]
    public async Task Realm_QuotaProperties_CanBeUpdated()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();

        var realm = new Realm
        {
            Id = realmId,
            Name = "Test Realm",
            Slug = "test-realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            MaxContainers = 10,
            MaxDatabases = 5
        };

        db.Realms.Add(realm);
        await db.SaveChangesAsync();

        // Update quota properties
        realm.MaxContainers = 20;
        realm.MaxDatabases = 10;
        realm.MaxManagedApps = 30;
        realm.MaxRamBytes = 16L * 1024 * 1024 * 1024;
        realm.MaxCpuCores = 8.0;

        await db.SaveChangesAsync();

        var retrieved = await db.Realms.FindAsync(realmId);
        Assert.NotNull(retrieved);
        Assert.Equal(20, retrieved!.MaxContainers);
        Assert.Equal(10, retrieved.MaxDatabases);
        Assert.Equal(30, retrieved.MaxManagedApps);
        Assert.Equal(16L * 1024 * 1024 * 1024, retrieved.MaxRamBytes);
        Assert.Equal(8.0, retrieved.MaxCpuCores);
    }
}
