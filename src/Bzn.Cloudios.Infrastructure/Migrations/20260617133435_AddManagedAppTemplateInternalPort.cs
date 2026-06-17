using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bzn.Cloudios.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedAppTemplateInternalPort : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InternalPort",
                table: "ManagedAppTemplates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 80);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InternalPort",
                table: "ManagedAppTemplates");
        }
    }
}
