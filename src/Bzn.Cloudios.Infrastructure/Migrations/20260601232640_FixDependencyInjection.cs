using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bzn.Cloudios.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDependencyInjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Realms",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BillingPeriods",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContainerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StoppedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Hours = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.0),
                    CostBRL = table.Column<decimal>(type: "REAL", nullable: false, defaultValue: 0.0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPeriods", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPeriods_ContainerId_StartedAtUtc",
                table: "BillingPeriods",
                columns: new[] { "ContainerId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPeriods_StartedAtUtc",
                table: "BillingPeriods",
                column: "StartedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingPeriods");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Realms");
        }
    }
}
