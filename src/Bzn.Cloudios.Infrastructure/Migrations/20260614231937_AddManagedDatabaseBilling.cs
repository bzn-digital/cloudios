using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bzn.Cloudios.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedDatabaseBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ContainerId",
                table: "BillingPeriods",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "ManagedDatabaseId",
                table: "BillingPeriods",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingPeriods_ManagedDatabaseId_StartedAtUtc",
                table: "BillingPeriods",
                columns: new[] { "ManagedDatabaseId", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BillingPeriods_ManagedDatabaseId_StartedAtUtc",
                table: "BillingPeriods");

            migrationBuilder.DropColumn(
                name: "ManagedDatabaseId",
                table: "BillingPeriods");

            migrationBuilder.AlterColumn<Guid>(
                name: "ContainerId",
                table: "BillingPeriods",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
