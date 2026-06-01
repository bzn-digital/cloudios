using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bzn.Cloudios.Infrastructure.Migrations.MetricsDb
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContainerMetrics_History",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContainerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CpuPercent = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.0),
                    MemoryUsedBytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    NetworkRxBytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    NetworkTxBytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    BlockReadBytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    BlockWriteBytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerMetrics_History", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMetrics_History_ContainerId_Timestamp",
                table: "ContainerMetrics_History",
                columns: new[] { "ContainerId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMetrics_History_Timestamp",
                table: "ContainerMetrics_History",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContainerMetrics_History");
        }
    }
}
