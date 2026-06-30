using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bzn.Cloudios.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnType("TEXT");

        builder.Property(u => u.RealmId)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnType("TEXT");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(u => u.Role)
            .IsRequired()
            .HasColumnType("TEXT")
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_Users_Role",
            "Role IN ('PlatformAdmin','PlatformUser','PlatformSre','RealmOwner','RealmAdmin','RealmUser','RealmSre')"));

        builder.Property(u => u.IsBlocked)
            .IsRequired()
            .HasColumnType("INTEGER")
            .HasDefaultValue(false);

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.HasIndex(u => new { u.RealmId, u.Email }).IsUnique();
        builder.HasIndex(u => u.RealmId);
        builder.HasIndex(u => new { u.RealmId, u.Role });
    }
}
