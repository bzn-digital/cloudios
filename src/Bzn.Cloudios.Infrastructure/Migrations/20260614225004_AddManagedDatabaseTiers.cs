using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bzn.Cloudios.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedDatabaseTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatabaseTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CpuLimitCores = table.Column<double>(type: "REAL", nullable: false),
                    MemoryLimitBytes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManagedDatabaseInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RealmId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TierId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    NetworkId = table.Column<string>(type: "TEXT", nullable: false),
                    CpuLimit = table.Column<double>(type: "REAL", nullable: false),
                    MemoryLimit = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Provisioning"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagedDatabaseInstances", x => x.Id);
                    table.CheckConstraint("CK_ManagedDatabaseInstances_Status", "Status IN ('Provisioning','Running','Stopped','Failed')");
                    table.CheckConstraint("CK_ManagedDatabaseInstances_Type", "Type IN ('MySQL','MongoDB')");
                    table.ForeignKey(
                        name: "FK_ManagedDatabaseInstances_DatabaseTiers_TierId",
                        column: x => x.TierId,
                        principalTable: "DatabaseTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagedDatabaseInstances_Realms_RealmId",
                        column: x => x.RealmId,
                        principalTable: "Realms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DatabaseTiers",
                columns: new[] { "Id", "CpuLimitCores", "MemoryLimitBytes", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000101"), 0.5, 524288000L, "dbl-micro-1s" },
                    { new Guid("00000000-0000-0000-0000-000000000102"), 0.5, 1073741824L, "dbl-micro-2s" },
                    { new Guid("00000000-0000-0000-0000-000000000103"), 1.0, 1073741824L, "dbl-mini-1s" },
                    { new Guid("00000000-0000-0000-0000-000000000104"), 2.0, 1073741824L, "dbl-mini-2s" },
                    { new Guid("00000000-0000-0000-0000-000000000105"), 2.0, 2147483648L, "dbl-standard-1s" },
                    { new Guid("00000000-0000-0000-0000-000000000106"), 2.0, 4294967296L, "dbl-standard-2s" },
                    { new Guid("00000000-0000-0000-0000-000000000107"), 4.0, 4294967296L, "dbl-standard-3s" },
                    { new Guid("00000000-0000-0000-0000-000000000108"), 4.0, 8589934592L, "dbl-large-1s" },
                    { new Guid("00000000-0000-0000-0000-000000000109"), 8.0, 10737418240L, "dbl-large-2s" },
                    { new Guid("00000000-0000-0000-0000-000000000110"), 10.0, 12884901888L, "dbl-large-3s" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseTiers_Name",
                table: "DatabaseTiers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManagedDatabaseInstances_RealmId",
                table: "ManagedDatabaseInstances",
                column: "RealmId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedDatabaseInstances_RealmId_Name",
                table: "ManagedDatabaseInstances",
                columns: new[] { "RealmId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManagedDatabaseInstances_RealmId_Status",
                table: "ManagedDatabaseInstances",
                columns: new[] { "RealmId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ManagedDatabaseInstances_TierId",
                table: "ManagedDatabaseInstances",
                column: "TierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagedDatabaseInstances");

            migrationBuilder.DropTable(
                name: "DatabaseTiers");
        }
    }
}
