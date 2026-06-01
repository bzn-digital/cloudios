using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bzn.Cloudios.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Realms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Realms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Containers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RealmId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DockerContainerId = table.Column<string>(type: "TEXT", nullable: true),
                    ImageName = table.Column<string>(type: "TEXT", nullable: false),
                    InternalPort = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 8080),
                    Status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "Stopped"),
                    CpuLimitCores = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.5),
                    MemoryLimitBytes = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 536870912L),
                    CostPerHourBRL = table.Column<decimal>(type: "REAL", nullable: false, defaultValue: 0.02m),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Containers", x => x.Id);
                    table.CheckConstraint("CK_Containers_Status", "Status IN ('Deploying','Running','Stopped','Failed')");
                    table.ForeignKey(
                        name: "FK_Containers_Realms_RealmId",
                        column: x => x.RealmId,
                        principalTable: "Realms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RealmId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    IsBlocked = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_Role", "Role IN ('PlatformAdmin','PlatformUser','PlatformSre','RealmOwner','RealmAdmin','RealmUser','RealmSre')");
                    table.ForeignKey(
                        name: "FK_Users_Realms_RealmId",
                        column: x => x.RealmId,
                        principalTable: "Realms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContainerEnvVars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContainerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerEnvVars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContainerEnvVars_Containers_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContainerVolumes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContainerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HostPath = table.Column<string>(type: "TEXT", nullable: false),
                    ContainerPath = table.Column<string>(type: "TEXT", nullable: false),
                    IsReadOnly = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerVolumes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContainerVolumes_Containers_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContainerEnvVars_ContainerId",
                table: "ContainerEnvVars",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerEnvVars_ContainerId_Key",
                table: "ContainerEnvVars",
                columns: new[] { "ContainerId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Containers_DockerContainerId",
                table: "Containers",
                column: "DockerContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_Name",
                table: "Containers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_RealmId",
                table: "Containers",
                column: "RealmId");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_RealmId_Status",
                table: "Containers",
                columns: new[] { "RealmId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ContainerVolumes_ContainerId",
                table: "ContainerVolumes",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_Realms_Name",
                table: "Realms",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RealmId",
                table: "Users",
                column: "RealmId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RealmId_Role",
                table: "Users",
                columns: new[] { "RealmId", "Role" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContainerEnvVars");

            migrationBuilder.DropTable(
                name: "ContainerVolumes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Containers");

            migrationBuilder.DropTable(
                name: "Realms");
        }
    }
}
