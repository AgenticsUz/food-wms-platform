using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramPartners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "client_telegram_enabled",
                schema: "wms",
                table: "tenant",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "debt_reminder_days",
                schema: "wms",
                table: "tenant",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "counterparty_id",
                schema: "wms",
                table: "telegram_link",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "driver_id",
                schema: "wms",
                table: "telegram_link",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_telegram_link_counterparty_id",
                schema: "wms",
                table: "telegram_link",
                column: "counterparty_id");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_link_driver_id",
                schema: "wms",
                table: "telegram_link",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_link_tenant_id_counterparty_id",
                schema: "wms",
                table: "telegram_link",
                columns: new[] { "tenant_id", "counterparty_id" },
                unique: true,
                filter: "\"counterparty_id\" IS NOT NULL AND \"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_link_tenant_id_driver_id",
                schema: "wms",
                table: "telegram_link",
                columns: new[] { "tenant_id", "driver_id" },
                unique: true,
                filter: "\"driver_id\" IS NOT NULL AND \"is_deleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "ck_telegram_link_one_subject",
                schema: "wms",
                table: "telegram_link",
                sql: "(CASE WHEN \"user_profile_id\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"driver_id\" IS NULL THEN 0 ELSE 1 END + CASE WHEN \"counterparty_id\" IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.AddForeignKey(
                name: "fk_telegram_link_counterparty_counterparty_id",
                schema: "wms",
                table: "telegram_link",
                column: "counterparty_id",
                principalSchema: "wms",
                principalTable: "counterparty",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_telegram_link_driver_driver_id",
                schema: "wms",
                table: "telegram_link",
                column: "driver_id",
                principalSchema: "wms",
                principalTable: "driver",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_telegram_link_counterparty_counterparty_id",
                schema: "wms",
                table: "telegram_link");

            migrationBuilder.DropForeignKey(
                name: "fk_telegram_link_driver_driver_id",
                schema: "wms",
                table: "telegram_link");

            migrationBuilder.DropIndex(
                name: "ix_telegram_link_counterparty_id",
                schema: "wms",
                table: "telegram_link");

            migrationBuilder.DropIndex(
                name: "ix_telegram_link_driver_id",
                schema: "wms",
                table: "telegram_link");

            migrationBuilder.DropIndex(
                name: "ix_telegram_link_tenant_id_counterparty_id",
                schema: "wms",
                table: "telegram_link");

            migrationBuilder.DropIndex(
                name: "ix_telegram_link_tenant_id_driver_id",
                schema: "wms",
                table: "telegram_link");

            migrationBuilder.DropCheckConstraint(
                name: "ck_telegram_link_one_subject",
                schema: "wms",
                table: "telegram_link");

            migrationBuilder.DropColumn(
                name: "client_telegram_enabled",
                schema: "wms",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "debt_reminder_days",
                schema: "wms",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "counterparty_id",
                schema: "wms",
                table: "telegram_link");

            migrationBuilder.DropColumn(
                name: "driver_id",
                schema: "wms",
                table: "telegram_link");
        }
    }
}
