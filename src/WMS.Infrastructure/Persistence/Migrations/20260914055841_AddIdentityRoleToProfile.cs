using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityRoleToProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "identity_role",
                schema: "wms",
                table: "user_profile",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);


            // ⚠️ Bu yerda backfill YO'Q va bu ataylab. Birinchi urinishda shu ustunni
            // mavjud tizim rolidan to'ldiradigan UPDATE yozilgan edi — u PROD'DA JIM
            // 0 QATORGA tegdi: `user_profile` va `user_role` da RLS `FORCE` rejimida,
            // migratsiya esa `app_migrator` roli bilan ketadi (`rolbypassrls = false`),
            // ya'ni tenant konteksti qo'yilmagan so'rov hech narsa ko'rmaydi.
            //
            // To'ldirishning HOJATI ham yo'q: `WmsPlatformUserSink.SyncRoleAsync`
            // `identity_role` bo'sh bo'lsa va profilda AYNAN bitta tizim roli bo'lsa,
            // o'shani «JIT bergan» deb qabul qiladi — eski profillar birinchi
            // kirishdayoq to'g'rilanadi va ustun o'sha yerda yoziladi.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "identity_role",
                schema: "wms",
                table: "user_profile");
        }
    }
}
