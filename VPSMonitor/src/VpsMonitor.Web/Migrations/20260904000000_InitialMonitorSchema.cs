using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpsMonitor.Web.Migrations;

public partial class InitialMonitorSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AuditEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Target = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                RequestIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                Success = table.Column<bool>(type: "boolean", nullable: false),
                Detail = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditEntries", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "HealthCheckDefinitions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                Method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HealthCheckDefinitions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "MonitorUsers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MonitorUsers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "MonitorSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                MonitorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MonitorSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_MonitorSessions_MonitorUsers_MonitorUserId",
                    column: x => x.MonitorUserId,
                    principalTable: "MonitorUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ProjectAssignments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ProjectKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                MonitorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProjectAssignments", x => x.Id);
                table.ForeignKey(
                    name: "FK_ProjectAssignments_MonitorUsers_MonitorUserId",
                    column: x => x.MonitorUserId,
                    principalTable: "MonitorUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AuditEntries_OccurredAtUtc",
            table: "AuditEntries",
            column: "OccurredAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_HealthCheckDefinitions_ProjectKey",
            table: "HealthCheckDefinitions",
            column: "ProjectKey");

        migrationBuilder.CreateIndex(
            name: "IX_MonitorSessions_MonitorUserId",
            table: "MonitorSessions",
            column: "MonitorUserId");

        migrationBuilder.CreateIndex(
            name: "IX_MonitorSessions_TokenHash",
            table: "MonitorSessions",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MonitorUsers_Username",
            table: "MonitorUsers",
            column: "Username",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ProjectAssignments_MonitorUserId",
            table: "ProjectAssignments",
            column: "MonitorUserId");

        migrationBuilder.CreateIndex(
            name: "IX_ProjectAssignments_ProjectKey",
            table: "ProjectAssignments",
            column: "ProjectKey");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AuditEntries");
        migrationBuilder.DropTable(name: "HealthCheckDefinitions");
        migrationBuilder.DropTable(name: "MonitorSessions");
        migrationBuilder.DropTable(name: "ProjectAssignments");
        migrationBuilder.DropTable(name: "MonitorUsers");
    }
}
