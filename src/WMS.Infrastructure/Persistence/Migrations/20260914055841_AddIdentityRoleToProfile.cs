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

            // Mavjud profillar: JIT ularga birinchi kirishda AYNAN BITTA tizim rolini
            // bergan edi. Ustunni o'sha rol bilan to'ldiramiz — aks holda birinchi
            // sinxronizatsiya «avvalgisi noma'lum» deb ikkinchi tizim rolini qo'shib
            // yuborardi. Bir nechta tizim roli bo'lsa (admin qo'shgan) — tegilmaydi,
            // sink ularni ongli qaror deb hisoblaydi.
            migrationBuilder.Sql("""
                UPDATE wms.user_profile p
                SET identity_role = s.code
                FROM (
                    SELECT ur.user_id, MIN(r.code) AS code, COUNT(*) AS cnt
                    FROM wms.user_role ur
                    JOIN wms.role r ON r.id = ur.role_id
                    WHERE r.code IN ('admin', 'manager', 'employee', 'viewer')
                      AND r.is_deleted = false
                    GROUP BY ur.user_id
                ) s
                WHERE s.user_id = p.id AND s.cnt = 1;
                """);
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
