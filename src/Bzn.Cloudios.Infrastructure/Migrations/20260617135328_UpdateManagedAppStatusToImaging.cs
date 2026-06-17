using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bzn.Cloudios.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateManagedAppStatusToImaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ManagedAppInstances_Status",
                table: "ManagedAppInstances");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ManagedAppInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: "Imaging",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldDefaultValue: "Provisioning");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ManagedAppInstances_Status",
                table: "ManagedAppInstances",
                sql: "Status IN ('Imaging','Running','Stopped','Failed','Terminated')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ManagedAppInstances_Status",
                table: "ManagedAppInstances");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ManagedAppInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: "Provisioning",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldDefaultValue: "Imaging");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ManagedAppInstances_Status",
                table: "ManagedAppInstances",
                sql: "Status IN ('Provisioning','Running','Stopped','Failed','Terminated')");
        }
    }
}
