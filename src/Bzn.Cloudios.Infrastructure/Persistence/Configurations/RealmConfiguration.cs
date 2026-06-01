using Bzn.Cloudios.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bzn.Cloudios.Infrastructure.Persistence.Configurations;

public sealed class RealmConfiguration : IEntityTypeConfiguration<Realm>
{
    public void Configure(EntityTypeBuilder<Realm> builder)
    {
        builder.ToTable("Realms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnType("TEXT");

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("TEXT");

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasColumnType("INTEGER")
            .HasDefaultValue(true);

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.HasIndex(r => r.Name).IsUnique();

        builder.HasMany(r => r.Users)
            .WithOne(u => u.Realm)
            .HasForeignKey(u => u.RealmId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Containers)
            .WithOne(c => c.Realm)
            .HasForeignKey(c => c.RealmId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
