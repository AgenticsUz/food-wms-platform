using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// P2.1 — nom qidiruvi: <c>name_search</c> ustunlari, GIN trigram indekslari va
    /// mavjud qatorlar uchun backfill.
    /// </summary>
    public partial class F10_NameSearch : Migration
    {
        /// <summary>Qidiruv ustuni to'ldiriladigan jadvallar (hammasi tenant jadvali — RLS ostida).</summary>
        private static readonly string[] Tables = ["product", "counterparty", "warehouse"];

        /// <summary>
        /// <c>SearchNormalizer.Normalize</c> ning SQL nusxasi — faqat BACKFILL uchun.
        /// </summary>
        /// <remarks>
        /// ⚠️ Ish vaqtida ustunni C# to'ldiradi; bu ifoda mavjud qatorlarni bir martaga
        /// to'g'rilaydi. Ikkisi BIR XIL qoidada bo'lishi shart:
        /// 1) kichik harf; 2) bir belgidan uzun kirill harflari (ё→yo, ц→ts, ч→ch, ш/щ→sh,
        /// ю→yu, я→ya); 3) qolgan kirill harflari birma-bir (ъ va ь — tashlanadi);
        /// 4) apostrofning hamma ko'rinishi tashlanadi («bo‘g‘irsoq» → «bogirsoq»);
        /// 5) qolgan hamma belgi bo'sh joy va ketma-ketlari bittaga siqiladi; 6) 400 belgi.
        /// </remarks>
        private const string NormalizeSql = """
            left(btrim(regexp_replace(
                translate(
                    translate(
                        replace(replace(replace(replace(replace(replace(replace(
                            lower(name),
                            'ё', 'yo'), 'ц', 'ts'), 'ч', 'ch'), 'ш', 'sh'), 'щ', 'sh'), 'ю', 'yu'), 'я', 'ya'),
                        'абвгғдежзийкқлмнопрўстуфхҳыэъь',
                        'abvggdejziykqlmnoprostufxhie'),
                    '''' || chr(699) || chr(700) || chr(8216) || chr(8217) || chr(96) || chr(180),
                    ''),
                '[^a-z0-9]+', ' ', 'g')), 400)
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Prod'da init skripti (`docker/postgres/init/01-roles-schemas.sql`) va testda fixture
            // buni allaqachon yaratadi; bu yerdagi nusxa migratsiyani O'ZI YETARLI qiladi.
            // ⚠️ `IF NOT EXISTS` — Postgres kengaytma BORLIGINI huquq tekshiruvidan OLDIN
            // ko'radi, ya'ni `app_migrator` da `CREATE` huquqi bo'lmasa ham xato bermaydi.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.AddColumn<string>(
                name: "name_search",
                schema: "wms",
                table: "warehouse",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "name_search",
                schema: "wms",
                table: "product",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "name_search",
                schema: "wms",
                table: "counterparty",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            BackfillNameSearch(migrationBuilder);

            // Indeks backfill'dan KEYIN — bo'sh ustunni indekslab, keyin har qatorni yangilash
            // indeksni ikki marta yozdirardi.
            migrationBuilder.CreateIndex(
                name: "ix_warehouse_name_search",
                schema: "wms",
                table: "warehouse",
                column: "name_search")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_product_name_search",
                schema: "wms",
                table: "product",
                column: "name_search")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_counterparty_name_search",
                schema: "wms",
                table: "counterparty",
                column: "name_search")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <summary>
        /// Mavjud qatorlarni to'ldiradi — RLS'ni CHETLAB o'tib.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ⚠️ <b>Saboq (XATOLAR-2026-09-14 §9.3, T1).</b> Migratsiya <c>app_migrator</c> roli
        /// bilan ketadi (<c>rolbypassrls = false</c>), tenant jadvallarida esa RLS
        /// <c>FORCE</c> rejimida va <c>app.tenant_id</c> qo'yilmagan. Oddiy <c>UPDATE</c> shu
        /// sababli prod'da JIM 0 qatorga tegadi — xato ham bermaydi. Bitta tenantni
        /// <c>set_config</c> bilan qo'yish ham yaramaydi: tenantlar ko'p.
        /// </para>
        /// <para>
        /// <b>Nega <c>NO FORCE</c>.</b> <c>FORCE</c> ni vaqtincha olib tashlash jadval EGASIGA
        /// (aynan <c>app_migrator</c> ga) siyosatni qo'llamaydi, ish vaqtidagi <c>app_user</c>
        /// esa hech narsa sezmaydi: <c>ENABLE ROW LEVEL SECURITY</c> va
        /// <c>tenant_isolation</c> siyosati JOYIDA qoladi, ya'ni izolyatsiya bir lahza ham
        /// ochilmaydi. Migratsiya bitta tranzaksiyada ketgani va <c>ALTER TABLE</c> Postgres'da
        /// tranzaksion bo'lgani uchun yarim yo'lda uzilsa <c>FORCE</c> o'zi qaytadi.
        /// </para>
        /// <para>
        /// <b>Ko'rib chiqilgan muqobil</b> — <c>wms.tenant</c> bo'ylab aylanib har tenant uchun
        /// <c>set_config('app.tenant_id', …, true)</c> qo'yib <c>UPDATE</c> qilish. Rad etildi:
        /// u faqat <c>wms.tenant</c> da qatori BOR tenantlarni to'ldiradi — o'chirilgan yoki
        /// yetim tenantning mahsuloti jimgina bo'sh <c>name_search</c> bilan qolib ketardi,
        /// ya'ni xuddi T1 dagi «jim 0 qator» tuzog'ining yumshoqroq shakli.
        /// </para>
        /// </remarks>
        private static void BackfillNameSearch(MigrationBuilder migrationBuilder)
        {
            foreach (string table in Tables)
            {
                migrationBuilder.Sql($"ALTER TABLE wms.{table} NO FORCE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($"UPDATE wms.{table} SET name_search = {NormalizeSql};");
                migrationBuilder.Sql($"ALTER TABLE wms.{table} FORCE ROW LEVEL SECURITY;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_warehouse_name_search",
                schema: "wms",
                table: "warehouse");

            migrationBuilder.DropIndex(
                name: "ix_product_name_search",
                schema: "wms",
                table: "product");

            migrationBuilder.DropIndex(
                name: "ix_counterparty_name_search",
                schema: "wms",
                table: "counterparty");

            migrationBuilder.DropColumn(
                name: "name_search",
                schema: "wms",
                table: "warehouse");

            migrationBuilder.DropColumn(
                name: "name_search",
                schema: "wms",
                table: "product");

            migrationBuilder.DropColumn(
                name: "name_search",
                schema: "wms",
                table: "counterparty");

            // ⚠️ `pg_trgm` ATAYLAB o'chirilmaydi: uni init skripti ham yaratadi va boshqa
            // qidiruvlar ham unga tayanishi mumkin.
        }
    }
}
