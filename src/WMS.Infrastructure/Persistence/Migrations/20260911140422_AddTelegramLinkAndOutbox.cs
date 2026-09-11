using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramLinkAndOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "telegram_chat_id",
                schema: "wms",
                table: "user_profile");

            migrationBuilder.CreateTable(
                name: "telegram_link",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chat_id = table.Column<long>(type: "bigint", nullable: false),
                    username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    first_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    lang = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    linked_via = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    linked_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    muted_types = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    digest = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_link", x => x.id);
                    table.ForeignKey(
                        name: "fk_telegram_link_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_telegram_link_user_profile_user_profile_id",
                        column: x => x.user_profile_id,
                        principalSchema: "wms",
                        principalTable: "user_profile",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "telegram_link_token",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subject_type = table.Column<int>(type: "integer", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_link_token", x => x.id);
                    table.ForeignKey(
                        name: "fk_telegram_link_token_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "telegram_outbox",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    telegram_link_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chat_id = table.Column<long>(type: "bigint", nullable: false),
                    text = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    notification_id = table.Column<Guid>(type: "uuid", nullable: true),
                    dedup_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    last_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_outbox", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_telegram_link_chat_id",
                schema: "wms",
                table: "telegram_link",
                column: "chat_id");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_link_tenant_id_user_profile_id",
                schema: "wms",
                table: "telegram_link",
                columns: new[] { "tenant_id", "user_profile_id" },
                unique: true,
                filter: "\"user_profile_id\" IS NOT NULL AND \"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_link_user_profile_id",
                schema: "wms",
                table: "telegram_link",
                column: "user_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_link_token_subject_type_subject_id",
                schema: "wms",
                table: "telegram_link_token",
                columns: new[] { "subject_type", "subject_id" });

            migrationBuilder.CreateIndex(
                name: "ix_telegram_link_token_tenant_id",
                schema: "wms",
                table: "telegram_link_token",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_link_token_token",
                schema: "wms",
                table: "telegram_link_token",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_telegram_outbox_dedup_key",
                schema: "wms",
                table: "telegram_outbox",
                column: "dedup_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_telegram_outbox_status_next_attempt_at",
                schema: "wms",
                table: "telegram_outbox",
                columns: new[] { "status", "next_attempt_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "telegram_link",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "telegram_link_token",
                schema: "wms");

            migrationBuilder.DropTable(
                name: "telegram_outbox",
                schema: "wms");

            migrationBuilder.AddColumn<string>(
                name: "telegram_chat_id",
                schema: "wms",
                table: "user_profile",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }
    }
}
