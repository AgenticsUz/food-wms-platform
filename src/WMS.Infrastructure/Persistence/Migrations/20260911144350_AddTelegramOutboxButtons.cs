using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramOutboxButtons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "kind",
                schema: "wms",
                table: "telegram_outbox",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "message_id",
                schema: "wms",
                table: "telegram_outbox",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reply_markup",
                schema: "wms",
                table: "telegram_outbox",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_telegram_outbox_chat_id_message_id",
                schema: "wms",
                table: "telegram_outbox",
                columns: new[] { "chat_id", "message_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_telegram_outbox_chat_id_message_id",
                schema: "wms",
                table: "telegram_outbox");

            migrationBuilder.DropColumn(
                name: "kind",
                schema: "wms",
                table: "telegram_outbox");

            migrationBuilder.DropColumn(
                name: "message_id",
                schema: "wms",
                table: "telegram_outbox");

            migrationBuilder.DropColumn(
                name: "reply_markup",
                schema: "wms",
                table: "telegram_outbox");
        }
    }
}
