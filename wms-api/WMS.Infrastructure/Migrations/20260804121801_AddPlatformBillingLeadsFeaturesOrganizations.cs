using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformBillingLeadsFeaturesOrganizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note on the Organization backfill: Counterparty.Inn is introduced by THIS
            // migration, so no existing row can carry a taxpayer id and there is nothing to
            // match on yet. Organizations are created from now on, as INNs are entered or
            // imported (OrganizationMatcher). Older records simply stay unlinked until
            // someone fills the number in.

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Tenants",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidUntil",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspendNote",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspendPublicMessage",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuspendReason",
                table: "Tenants",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspendedAt",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuspendedByUserId",
                table: "Tenants",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspendedUntil",
                table: "Tenants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeatureCodes",
                table: "Plans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Inn",
                table: "Counterparties",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Counterparties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ModuleCode = table.Column<string>(type: "TEXT", nullable: true),
                    DefaultEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCustom = table.Column<bool>(type: "INTEGER", nullable: false),
                    OwnerTenantId = table.Column<int>(type: "INTEGER", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyName = table.Column<string>(type: "TEXT", nullable: false),
                    ContactName = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    ReferrerTenantId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StatusNote = table.Column<string>(type: "TEXT", nullable: true),
                    ConvertedTenantId = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceCounterpartyId = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceAgentId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Inn = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<double>(type: "REAL", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    RecordedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenantFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<int>(type: "INTEGER", nullable: false),
                    FeatureCode = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    SetByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    SetAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantFeatures_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_OrganizationId",
                table: "Tenants",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Counterparties_OrganizationId",
                table: "Counterparties",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Features_Code",
                table: "Features",
                column: "Code",
                unique: true,
                filter: "\"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Phone",
                table: "Leads",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Status",
                table: "Leads",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Inn",
                table: "Organizations",
                column: "Inn",
                unique: true,
                filter: "\"Inn\" IS NOT NULL AND \"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_TenantId_PeriodEnd",
                table: "PaymentRecords",
                columns: new[] { "TenantId", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantFeatures_TenantId_FeatureCode",
                table: "TenantFeatures",
                columns: new[] { "TenantId", "FeatureCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Counterparties_Organizations_OrganizationId",
                table: "Counterparties",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_Organizations_OrganizationId",
                table: "Tenants",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Counterparties_Organizations_OrganizationId",
                table: "Counterparties");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_Organizations_OrganizationId",
                table: "Tenants");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropTable(
                name: "Leads");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "PaymentRecords");

            migrationBuilder.DropTable(
                name: "TenantFeatures");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_OrganizationId",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Counterparties_OrganizationId",
                table: "Counterparties");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PaidUntil",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SuspendNote",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SuspendPublicMessage",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SuspendReason",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SuspendedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SuspendedByUserId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SuspendedUntil",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FeatureCodes",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "Inn",
                table: "Counterparties");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Counterparties");
        }
    }
}
