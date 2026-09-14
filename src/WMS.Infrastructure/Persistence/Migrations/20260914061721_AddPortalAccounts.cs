using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPortalAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "identity_sub",
                schema: "wms",
                table: "counterparty",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "identity_sub",
                schema: "wms",
                table: "agent",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_counterparty_tenant_id_identity_sub",
                schema: "wms",
                table: "counterparty",
                columns: new[] { "tenant_id", "identity_sub" },
                unique: true,
                filter: "\"identity_sub\" IS NOT NULL AND \"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_agent_tenant_id_identity_sub",
                schema: "wms",
                table: "agent",
                columns: new[] { "tenant_id", "identity_sub" },
                unique: true,
                filter: "\"identity_sub\" IS NOT NULL AND \"is_deleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_counterparty_tenant_id_identity_sub",
                schema: "wms",
                table: "counterparty");

            migrationBuilder.DropIndex(
                name: "ix_agent_tenant_id_identity_sub",
                schema: "wms",
                table: "agent");

            migrationBuilder.DropColumn(
                name: "identity_sub",
                schema: "wms",
                table: "counterparty");

            migrationBuilder.DropColumn(
                name: "identity_sub",
                schema: "wms",
                table: "agent");
        }
    }
}
