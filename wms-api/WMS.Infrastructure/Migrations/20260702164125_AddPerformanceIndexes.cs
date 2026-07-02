using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_TenantId",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_TenantId_WarehouseId_ProductId",
                table: "WarehouseStocks",
                columns: new[] { "TenantId", "WarehouseId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_TenantId_ConfirmedAt",
                table: "Transfers",
                columns: new[] { "TenantId", "ConfirmedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_TenantId_Status",
                table: "Transfers",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TenantId_Date",
                table: "Transactions",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftPlans_TenantId_Date",
                table: "ShiftPlans",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftActuals_TenantId_Date",
                table: "ShiftActuals",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId",
                table: "Products",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "TenantId", "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Counterparties_TenantId",
                table: "Counterparties",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CommissionRecords_TenantId_AgentId",
                table: "CommissionRecords",
                columns: new[] { "TenantId", "AgentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Batches_ExpiryDate",
                table: "Batches",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_TenantId_ProductId",
                table: "Batches",
                columns: new[] { "TenantId", "ProductId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WarehouseStocks_TenantId_WarehouseId_ProductId",
                table: "WarehouseStocks");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_TenantId_ConfirmedAt",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_TenantId_Status",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_TenantId_Date",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_ShiftPlans_TenantId_Date",
                table: "ShiftPlans");

            migrationBuilder.DropIndex(
                name: "IX_ShiftActuals_TenantId_Date",
                table: "ShiftActuals");

            migrationBuilder.DropIndex(
                name: "IX_Products_TenantId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TenantId_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Counterparties_TenantId",
                table: "Counterparties");

            migrationBuilder.DropIndex(
                name: "IX_CommissionRecords_TenantId_AgentId",
                table: "CommissionRecords");

            migrationBuilder.DropIndex(
                name: "IX_Batches_ExpiryDate",
                table: "Batches");

            migrationBuilder.DropIndex(
                name: "IX_Batches_TenantId_ProductId",
                table: "Batches");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId",
                table: "Notifications",
                column: "TenantId");
        }
    }
}
