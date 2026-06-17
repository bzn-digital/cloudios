using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace Bzn.Cloudios.Infrastructure.Persistence.Configurations;

public sealed class ManagedAppTemplateConfiguration : IEntityTypeConfiguration<ManagedAppTemplate>
{
    public void Configure(EntityTypeBuilder<ManagedAppTemplate> builder)
    {
        builder.ToTable("ManagedAppTemplates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnType("TEXT");

        builder.Property(t => t.Slug)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("TEXT");

        builder.Property(t => t.DisplayName)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("TEXT");

        builder.Property(t => t.Description)
            .HasColumnType("TEXT");

        builder.Property(t => t.Category)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("TEXT");

        builder.Property(t => t.DockerImage)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(t => t.DefaultEnvVars)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new(),
                new ValueComparer<Dictionary<string, string>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
                    c => c.ToDictionary(d => d.Key, d => d.Value)));

        builder.Property(t => t.DefaultInstanceSize)
            .IsRequired()
            .HasColumnType("TEXT")
            .HasConversion<string>()
            .HasDefaultValue(InstanceSize.Micro1s);

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(t => t.UpdatedAt)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.HasIndex(t => t.Slug).IsUnique();
    }
}
