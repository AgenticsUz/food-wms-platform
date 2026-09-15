using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// A0: AI poydevori — suhbat/xabar tarixi, kunlik sarf hisobi va audit havolalari.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ai_usage</c> tenant bo'yicha (RLS), <c>ai_daily_cost</c> esa PLATFORMA bo'yicha
    /// (tenant ustuni yo'q): kunlik dollar shifti hamma tenant yig'indisiga qaraydi va RLS
    /// ostidagi jadvaldan bunday yig'indini olib bo'lmasdi.
    /// </para>
    /// <para>
    /// Backfill YO'Q: hamma yangi ustun <c>nullable</c> yoki sukut qiymatli, ya'ni migratsiya
    /// tenant jadvallariga YOZMAYDI (XATOLAR §9.3 saboqi — <c>app_migrator</c> RLS ostidagi
    /// qatorlarni ko'rmaydi).
    /// </para>
    /// </remarks>
    public partial class F10_AiFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ai_conversation_id",
                schema: "wms",
                table: "transfer",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_ai_requests_per_month",
                schema: "wms",
                table: "plan",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ai_conversation_id",
                schema: "wms",
                table: "payment_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_conversation",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    user_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    telegram_chat_id = table.Column<long>(type: "bigint", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    last_activity_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_conversation", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_conversation_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ai_conversation_user_profile_user_profile_id",
                        column: x => x.user_profile_id,
                        principalSchema: "wms",
                        principalTable: "user_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ai_daily_cost",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    requests = table.Column<int>(type: "integer", nullable: false),
                    usd_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_daily_cost", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_usage",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    requests = table.Column<int>(type: "integer", nullable: false),
                    input_tokens = table.Column<long>(type: "bigint", nullable: false),
                    output_tokens = table.Column<long>(type: "bigint", nullable: false),
                    cache_write_tokens = table.Column<long>(type: "bigint", nullable: false),
                    cache_read_tokens = table.Column<long>(type: "bigint", nullable: false),
                    usd_cost = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_usage", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_usage_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ai_message",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "text", nullable: true),
                    tool_calls_json = table.Column<string>(type: "jsonb", nullable: true),
                    tool_results_json = table.Column<string>(type: "jsonb", nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    input_tokens = table.Column<int>(type: "integer", nullable: false),
                    output_tokens = table.Column<int>(type: "integer", nullable: false),
                    cache_write_tokens = table.Column<int>(type: "integer", nullable: false),
                    cache_read_tokens = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_message", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_message_ai_conversation_conversation_id",
                        column: x => x.conversation_id,
                        principalSchema: "wms",
                        principalTable: "ai_conversation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ai_message_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_transfer_ai_conversation_id",
                schema: "wms",
                table: "transfer",
                column: "ai_conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_history_ai_conversation_id",
                schema: "wms",
                table: "payment_history",
                column: "ai_conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_conversation_telegram_chat_id_last_activity_at",
                schema: "wms",
                table: "ai_conversation",
                columns: new[] { "telegram_chat_id", "last_activity_at" },
                filter: "\"telegram_chat_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ai_conversation_tenant_id_last_activity_at",
                schema: "wms",
                table: "ai_conversation",
                columns: new[] { "tenant_id", "last_activity_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_conversation_user_profile_id",
                schema: "wms",
                table: "ai_conversation",
                column: "user_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_daily_cost_day",
                schema: "wms",
                table: "ai_daily_cost",
                column: "day",
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_ai_message_conversation_id_sequence",
                schema: "wms",
                table: "ai_message",
                columns: new[] { "conversation_id", "sequence" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_ai_message_tenant_id",
                schema: "wms",
                table: "ai_message",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_usage_tenant_id_day_model",
                schema: "wms",
                table: "ai_usage",
                columns: new[] { "tenant_id", "day", "model" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "fk_payment_history_ai_conversation_ai_conversation_id",
                schema: "wms",
                table: "payment_history",
                column: "ai_conversation_id",
                principalSchema: "wms",
                principalTable: "ai_conversation",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_transfer_ai_conversation_ai_conversation_id",
                schema: "wms",
                table: "transfer",
                column: "ai_conversation_id",
                principalSchema: "wms",
                principalTable: "ai_conversation",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_history_ai_conversation_ai_conversation_id",
                schema: "wms",
                table: "payment_history");

            migrationBuilder.DropForeignKey(
                name: "fk_transfer_ai_conversation_ai_conversation_id",
                schema: "wms",
                table: "transfer");

            migrationBuilder.DropTable(
                name: "ai_daily_cost",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "ai_message",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "ai_usage",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "ai_conversation",
                schema: "wms");

            migrationBuilder.DropIndex(
                name: "ix_transfer_ai_conversation_id",
                schema: "wms",
                table: "transfer");

            migrationBuilder.DropIndex(
                name: "ix_payment_history_ai_conversation_id",
                schema: "wms",
                table: "payment_history");

            migrationBuilder.DropColumn(
                name: "ai_conversation_id",
                schema: "wms",
                table: "transfer");

            migrationBuilder.DropColumn(
                name: "max_ai_requests_per_month",
                schema: "wms",
                table: "plan");

            migrationBuilder.DropColumn(
                name: "ai_conversation_id",
                schema: "wms",
                table: "payment_history");
        }
    }
}
