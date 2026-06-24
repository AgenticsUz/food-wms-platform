using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AgentId",
                table: "Transfers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CommissionPercent",
                table: "Transfers",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AgentId",
                table: "Counterparties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    CommissionPercent = table.Column<double>(type: "REAL", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    PortalPhone = table.Column<string>(type: "TEXT", nullable: true),
                    PortalPasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    PortalEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CommissionRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    AgentId = table.Column<int>(type: "INTEGER", nullable: false),
                    TransferId = table.Column<int>(type: "INTEGER", nullable: false),
                    SaleAmount = table.Column<double>(type: "REAL", nullable: false),
                    CommissionPercent = table.Column<double>(type: "REAL", nullable: false),
                    CommissionAmount = table.Column<double>(type: "REAL", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPaid = table.Column<bool>(type: "INTEGER", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommissionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommissionRecords_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommissionRecords_Transfers_TransferId",
                        column: x => x.TransferId,
                        principalTable: "Transfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsDeleted", "Module", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 23, "agents.view", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, "AGENTS", "View Agents", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 24, "agents.manage", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, "AGENTS", "Manage Agents", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_AgentId",
                table: "Transfers",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Counterparties_AgentId",
                table: "Counterparties",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_UserId",
                table: "Agents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRecords_AgentId",
                table: "CommissionRecords",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRecords_TransferId",
                table: "CommissionRecords",
                column: "TransferId");

            migrationBuilder.AddForeignKey(
                name: "FK_Counterparties_Agents_AgentId",
                table: "Counterparties",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transfers_Agents_AgentId",
                table: "Transfers",
                column: "AgentId",
                principalTable: "Agents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Counterparties_Agents_AgentId",
                table: "Counterparties");

            migrationBuilder.DropForeignKey(
                name: "FK_Transfers_Agents_AgentId",
                table: "Transfers");

            migrationBuilder.DropTable(
                name: "CommissionRecords");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_AgentId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Counterparties_AgentId",
                table: "Counterparties");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "CommissionPercent",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "Counterparties");
        }
    }
}
