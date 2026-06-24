using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bzn.Cloudios.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRealmQuotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxContainers",
                table: "Realms",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxCpuCores",
                table: "Realms",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxDatabases",
                table: "Realms",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxManagedApps",
                table: "Realms",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MaxRamBytes",
                table: "Realms",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxContainers",
                table: "Realms");

            migrationBuilder.DropColumn(
                name: "MaxCpuCores",
                table: "Realms");

            migrationBuilder.DropColumn(
                name: "MaxDatabases",
                table: "Realms");

            migrationBuilder.DropColumn(
                name: "MaxManagedApps",
                table: "Realms");

            migrationBuilder.DropColumn(
                name: "MaxRamBytes",
                table: "Realms");
        }
    }
}
