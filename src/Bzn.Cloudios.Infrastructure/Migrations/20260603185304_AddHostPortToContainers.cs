using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bzn.Cloudios.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHostPortToContainers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HostPort",
                table: "Containers",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HostPort",
                table: "Containers");
        }
    }
}
