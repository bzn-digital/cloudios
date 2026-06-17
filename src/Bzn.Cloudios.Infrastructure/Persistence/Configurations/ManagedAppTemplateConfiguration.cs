using Bzn.Cloudios.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200)
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
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new());

        builder.Property(t => t.DefaultInstanceSize)
            .IsRequired()
            .HasColumnType("TEXT")
            .HasConversion<string>();

        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(t => t.UpdatedAt)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.HasIndex(t => t.Slug).IsUnique();
    }
}
