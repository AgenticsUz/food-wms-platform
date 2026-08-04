using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaasEnforcement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Plans",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TrialDays",
                table: "Plans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ActorTenantId",
                table: "AuditLogs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPlatformAction",
                table: "AuditLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsDeleted", "Name", "OrderNumber", "UpdatedAt" },
                values: new object[,]
                {
                    { 10, "AGENTS", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, "Agents", 10, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, "DELIVERY", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, "Delivery", 11, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            // SubscriptionStatus ustuni qo'shilgunga qadar yaratilgan tenantlarda qiymat 0
            // (enum'da yo'q) bo'lib qolgan. Obuna endi majburlanadigani uchun ular aniq
            // Active (2) holatiga keltiriladi.
            migrationBuilder.Sql("UPDATE Tenants SET SubscriptionStatus = 2 WHERE SubscriptionStatus = 0;");

            // Unique indeks qo'yishdan oldin mavjud dublikatlarni yechamiz: eng eski yozuv
            // slug'ini saqlab qoladi, keyingilariga "-dupN" qo'shiladi. Ma'lumot o'chmaydi —
            // aks holda migration dublikat bo'lgan bazada yiqilardi.
            migrationBuilder.Sql(@"
                UPDATE Tenants SET Slug = Slug || '-dup' || Id
                WHERE IsDeleted = 0 AND Id NOT IN (
                    SELECT MIN(Id) FROM Tenants WHERE IsDeleted = 0 GROUP BY Slug);");

            migrationBuilder.Sql(@"
                UPDATE Plans SET Code = Code || '-dup' || Id
                WHERE IsDeleted = 0 AND Id NOT IN (
                    SELECT MIN(Id) FROM Plans WHERE IsDeleted = 0 GROUP BY Code);");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true,
                filter: "\"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_Code",
                table: "Plans",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Plans_Code",
                table: "Plans");

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "TrialDays",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "ActorTenantId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "IsPlatformAction",
                table: "AuditLogs");
        }
    }
}
