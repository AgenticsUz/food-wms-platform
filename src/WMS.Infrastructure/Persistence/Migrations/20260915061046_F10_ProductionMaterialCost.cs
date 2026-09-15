using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// P2.5 dumi: bosqichda sarflangan xomashyo QIYMATI saqlanadi.
    /// </summary>
    /// <remarks>
    /// Usiz tayyor mahsulot partiyasi tannarxsiz qolardi: xomashyo bosqich bajarilganda
    /// (alohida so'rovda) sarflanadi, partiya esa buyurtma yakunlanganda tug'iladi va
    /// oradagi qiymatni saqlaydigan joy yo'q edi. Backfill YO'Q — eski bosqichlarning
    /// sarfi hech qayerda yozilmagan, taxmin qilish esa foydani jimgina buzardi
    /// (tannarxi noma'lum qatorlar hisobotda ALOHIDA ko'rsatiladi).
    /// </remarks>
    public partial class F10_ProductionMaterialCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "material_cost",
                schema: "wms",
                table: "stage_execution",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "material_cost",
                schema: "wms",
                table: "stage_execution");
        }
    }
}
