using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bzn.Cloudios.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManagedApps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManagedAppTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DockerImage = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultEnvVars = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultInstanceSize = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Micro1s"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagedAppTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManagedAppInstances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RealmId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TemplateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    HostPort = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Provisioning"),
                    Size = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Micro1s"),
                    DockerContainerId = table.Column<string>(type: "TEXT", nullable: true),
                    CpuLimitCores = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.5),
                    MemoryLimitBytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 536870912L),
                    CostPerHourBRL = table.Column<decimal>(type: "REAL", nullable: false, defaultValue: 0.02m),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StoppedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagedAppInstances", x => x.Id);
                    table.CheckConstraint("CK_ManagedAppInstances_Status", "Status IN ('Provisioning','Running','Stopped','Failed','Terminated')");
                    table.ForeignKey(
                        name: "FK_ManagedAppInstances_ManagedAppTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ManagedAppTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManagedAppInstances_Realms_RealmId",
                        column: x => x.RealmId,
                        principalTable: "Realms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManagedAppInstances_DockerContainerId",
                table: "ManagedAppInstances",
                column: "DockerContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedAppInstances_HostPort",
                table: "ManagedAppInstances",
                column: "HostPort",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManagedAppInstances_RealmId",
                table: "ManagedAppInstances",
                column: "RealmId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedAppInstances_RealmId_Name",
                table: "ManagedAppInstances",
                columns: new[] { "RealmId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManagedAppInstances_TemplateId",
                table: "ManagedAppInstances",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ManagedAppTemplates_Slug",
                table: "ManagedAppTemplates",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManagedAppInstances");

            migrationBuilder.DropTable(
                name: "ManagedAppTemplates");
        }
    }
}
