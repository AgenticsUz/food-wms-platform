using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// P2.3–P2.9: hujjat sanasi va qisqa raqam, partiya tannarxi, qadoq, standart ombor,
    /// to'lov yo'nalishi/sanasi va storno.
    /// </summary>
    /// <remarks>
    /// Bitta migratsiya, chunki ustunlarning bir qismi BIR-BIRIGA bog'liq backfill talab
    /// qiladi (raqamlar → hisoblagich; hujjat sanasi → indeks) va ularni bo'lib yuborish
    /// oraliq holatda prod'ni noto'g'ri hisobot bilan qoldirardi.
    /// </remarks>
    public partial class F10_DocumentsAndCosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "default_warehouse_id",
                schema: "wms",
                table: "user_profile",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost",
                schema: "wms",
                table: "transfer_item",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "document_date",
                schema: "wms",
                table: "transfer",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "number",
                schema: "wms",
                table: "transfer",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "source",
                schema: "wms",
                table: "transfer",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "default_finished_warehouse_id",
                schema: "wms",
                table: "tenant",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "default_raw_warehouse_id",
                schema: "wms",
                table: "tenant",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "number",
                schema: "wms",
                table: "production_order",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "pack_size",
                schema: "wms",
                table: "product",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pack_unit",
                schema: "wms",
                table: "product",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "direction",
                schema: "wms",
                table: "payment_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "document_date",
                schema: "wms",
                table: "payment_history",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "reversal_of_id",
                schema: "wms",
                table: "payment_history",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reversal_reason",
                schema: "wms",
                table: "payment_history",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "source",
                schema: "wms",
                table: "payment_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "wms",
                table: "payment_history",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost",
                schema: "wms",
                table: "batch",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tenant_counter",
                schema: "wms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenant_counter", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenant_counter_tenant_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "wms",
                        principalTable: "tenant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // ⚠️ Backfill indekslardan OLDIN: `number` ustuni bo'yicha NOYOB indeks
            // qo'yilganda hamma qator 0 bo'lsa, ikkinchi qatorda darhol yiqilardi.
            BackfillDocuments(migrationBuilder);

            migrationBuilder.CreateIndex(
                name: "ix_user_profile_default_warehouse_id",
                schema: "wms",
                table: "user_profile",
                column: "default_warehouse_id");

            migrationBuilder.CreateIndex(
                name: "ix_transfer_tenant_id_document_date",
                schema: "wms",
                table: "transfer",
                columns: new[] { "tenant_id", "document_date" });

            migrationBuilder.CreateIndex(
                name: "ix_transfer_tenant_id_number",
                schema: "wms",
                table: "transfer",
                columns: new[] { "tenant_id", "number" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_production_order_tenant_id_number",
                schema: "wms",
                table: "production_order",
                columns: new[] { "tenant_id", "number" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_payment_history_reversal_of_id",
                schema: "wms",
                table: "payment_history",
                column: "reversal_of_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_history_tenant_id_document_date",
                schema: "wms",
                table: "payment_history",
                columns: new[] { "tenant_id", "document_date" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_history_tenant_id_reversal_of_id",
                schema: "wms",
                table: "payment_history",
                columns: new[] { "tenant_id", "reversal_of_id" },
                unique: true,
                filter: "\"reversal_of_id\" IS NOT NULL AND \"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_counter_tenant_id_kind",
                schema: "wms",
                table: "tenant_counter",
                columns: new[] { "tenant_id", "kind" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "fk_payment_history_payment_history_reversal_of_id",
                schema: "wms",
                table: "payment_history",
                column: "reversal_of_id",
                principalSchema: "wms",
                principalTable: "payment_history",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_user_profile_warehouse_default_warehouse_id",
                schema: "wms",
                table: "user_profile",
                column: "default_warehouse_id",
                principalSchema: "wms",
                principalTable: "warehouse",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_history_payment_history_reversal_of_id",
                schema: "wms",
                table: "payment_history");

            migrationBuilder.DropForeignKey(
                name: "fk_user_profile_warehouse_default_warehouse_id",
                schema: "wms",
                table: "user_profile");

            migrationBuilder.DropTable(
                name: "tenant_counter",
                schema: "wms");

            migrationBuilder.DropIndex(
                name: "ix_user_profile_default_warehouse_id",
                schema: "wms",
                table: "user_profile");

            migrationBuilder.DropIndex(
                name: "ix_transfer_tenant_id_document_date",
                schema: "wms",
                table: "transfer");

            migrationBuilder.DropIndex(
                name: "ix_transfer_tenant_id_number",
                schema: "wms",
                table: "transfer");

            migrationBuilder.DropIndex(
                name: "ix_production_order_tenant_id_number",
                schema: "wms",
                table: "production_order");

            migrationBuilder.DropIndex(
                name: "ix_payment_history_reversal_of_id",
                schema: "wms",
                table: "payment_history");

            migrationBuilder.DropIndex(
                name: "ix_payment_history_tenant_id_document_date",
                schema: "wms",
                table: "payment_history");

            migrationBuilder.DropIndex(
                name: "ix_payment_history_tenant_id_reversal_of_id",
                schema: "wms",
                table: "payment_history");

            migrationBuilder.DropColumn(
                name: "default_warehouse_id",
                schema: "wms",
                table: "user_profile");

            migrationBuilder.DropColumn(
                name: "unit_cost",
                schema: "wms",
                table: "transfer_item");

            migrationBuilder.DropColumn(
                name: "document_date",
                schema: "wms",
                table: "transfer");

            migrationBuilder.DropColumn(
                name: "number",
                schema: "wms",
                table: "transfer");

            migrationBuilder.DropColumn(
                name: "source",
                schema: "wms",
                table: "transfer");

            migrationBuilder.DropColumn(
                name: "default_finished_warehouse_id",
                schema: "wms",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "default_raw_warehouse_id",
                schema: "wms",
                table: "tenant");

            migrationBuilder.DropColumn(
                name: "number",
                schema: "wms",
                table: "production_order");

            migrationBuilder.DropColumn(
                name: "pack_size",
                schema: "wms",
                table: "product");

            migrationBuilder.DropColumn(
                name: "pack_unit",
                schema: "wms",
                table: "product");

            migrationBuilder.DropColumn(
                name: "direction",
                schema: "wms",
                table: "payment_history");

            migrationBuilder.DropColumn(
                name: "document_date",
                schema: "wms",
                table: "payment_history");

            migrationBuilder.DropColumn(
                name: "reversal_of_id",
                schema: "wms",
                table: "payment_history");

            migrationBuilder.DropColumn(
                name: "reversal_reason",
                schema: "wms",
                table: "payment_history");

            migrationBuilder.DropColumn(
                name: "source",
                schema: "wms",
                table: "payment_history");

            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "wms",
                table: "payment_history");

            migrationBuilder.DropColumn(
                name: "unit_cost",
                schema: "wms",
                table: "batch");
        }

        /// <summary>Mavjud qatorlarni to'ldiradi — RLS'ni CHETLAB o'tib.</summary>
        /// <remarks>
        /// <para>
        /// ⚠️ <b>Saboq (XATOLAR-2026-09-14 §9.3).</b> Migratsiya <c>app_migrator</c> roli bilan
        /// ketadi va tenant jadvallarida RLS <c>FORCE</c> rejimida: <c>app.tenant_id</c> yo'q
        /// bo'lgani uchun oddiy <c>UPDATE</c> JIMGINA 0 qatorga tegadi. Shuning uchun har
        /// jadval uchun <c>NO FORCE</c> → <c>UPDATE</c> → <c>FORCE</c> (F10_NameSearch bilan
        /// bir xil naqsh): siyosatning o'zi va <c>app_user</c> uchun izolyatsiya bir lahza
        /// ham ochilmaydi.
        /// </para>
        /// <para>
        /// Qiymatlar qayerdan:
        /// <list type="bullet">
        ///   <item>⚠️ Kun chegarasi OSHKORA UTC'da (<c>AT TIME ZONE 'UTC'</c>): yalang'och
        ///   <c>date_trunc</c> sessiya vaqt zonasiga qarab ishlaydi va prod sessiyasi UTC
        ///   bo'lmasa eski hujjatlar bir kunga siljib ketardi.</item>
        ///   <item><c>transfer.document_date</c> — tasdiqlangan sana, bo'lmasa yaratilgan sana
        ///   (hisobotlar ilgari <c>confirmed_at</c>, ro'yxatlar <c>created_at</c> bo'yicha
        ///   ishlardi — endi ikkalasi bitta ustunga qaraydi).</item>
        ///   <item><c>payment_history.document_date</c> — <c>paid_at</c>.</item>
        ///   <item><c>*.number</c> — tenant ichida yaratilish tartibida 1, 2, 3 …; hisoblagich
        ///   o'sha maksimumdan davom etadi.</item>
        ///   <item><c>batch.unit_cost</c> — partiyani TUG'DIRGAN kirim qatoridagi narx
        ///   (chiqim qatorlari hisobga olinmaydi: ular partiyani yaratmaydi).</item>
        ///   <item><c>source</c> — eski qatorlar ilovadan kirgan (<c>Ui = 1</c>).</item>
        /// </list>
        /// ⚠️ <c>payment_history.direction</c> eski qatorlarda NOMA'LUM: tizim uni saqlamagan.
        /// Hammasi <c>In = 1</c> (mijozdan tushum) deb belgilanadi — bu ustunlik qiladigan
        /// holat; qarz balansi backfill'dan O'ZGARMAYDI, ya'ni raqamlar buzilmaydi.
        /// </para>
        /// </remarks>
        /// <param name="migrationBuilder">Migratsiya quruvchisi.</param>
        private static void BackfillDocuments(MigrationBuilder migrationBuilder)
        {
            string[] tables = ["transfer", "payment_history", "production_order", "batch", "tenant_counter"];
            foreach (string table in tables)
            {
                migrationBuilder.Sql($"ALTER TABLE wms.{table} NO FORCE ROW LEVEL SECURITY;");
            }

            migrationBuilder.Sql("""
                UPDATE wms.transfer
                   SET document_date = date_trunc('day', COALESCE(confirmed_at, created_at) AT TIME ZONE 'UTC') AT TIME ZONE 'UTC',
                       source = 1;
                """);

            migrationBuilder.Sql("""
                UPDATE wms.payment_history
                   SET document_date = date_trunc('day', paid_at AT TIME ZONE 'UTC') AT TIME ZONE 'UTC',
                       direction = 1,
                       source = 1;
                """);

            migrationBuilder.Sql("""
                UPDATE wms.transfer t
                   SET number = seq.rn
                  FROM (SELECT id, row_number() OVER (PARTITION BY tenant_id ORDER BY created_at, id) AS rn
                          FROM wms.transfer) AS seq
                 WHERE seq.id = t.id;
                """);

            migrationBuilder.Sql("""
                UPDATE wms.production_order o
                   SET number = seq.rn
                  FROM (SELECT id, row_number() OVER (PARTITION BY tenant_id ORDER BY created_at, id) AS rn
                          FROM wms.production_order) AS seq
                 WHERE seq.id = o.id;
                """);

            migrationBuilder.Sql("""
                UPDATE wms.batch b
                   SET unit_cost = src.unit_price
                  FROM (SELECT DISTINCT ON (i.batch_id) i.batch_id, i.unit_price
                          FROM wms.transfer_item i
                          JOIN wms.transfer t ON t.id = i.transfer_id
                         WHERE i.batch_id IS NOT NULL AND i.unit_price > 0 AND t.type IN (1, 5)
                         ORDER BY i.batch_id, i.created_at) AS src
                 WHERE src.batch_id = b.id;
                """);

            // Hisoblagich mavjud maksimumdan davom etsin — aks holda keyingi hujjat
            // allaqachon band bo'lgan raqamni olib, noyob indeksga urilardi.
            migrationBuilder.Sql("""
                INSERT INTO wms.tenant_counter (id, tenant_id, kind, value, created_at, updated_at, is_deleted)
                SELECT gen_random_uuid(), tenant_id, 'transfer', MAX(number), now(), now(), false
                  FROM wms.transfer GROUP BY tenant_id;
                """);

            migrationBuilder.Sql("""
                INSERT INTO wms.tenant_counter (id, tenant_id, kind, value, created_at, updated_at, is_deleted)
                SELECT gen_random_uuid(), tenant_id, 'production_order', MAX(number), now(), now(), false
                  FROM wms.production_order GROUP BY tenant_id;
                """);

            foreach (string table in tables)
            {
                migrationBuilder.Sql($"ALTER TABLE wms.{table} FORCE ROW LEVEL SECURITY;");
            }
        }
    }
}
