/**
 * Analitika modellari — manba backend `DTOs/Analytics/AnalyticsDtos.cs`.
 *
 * Eskisidan farq: `TopDebtorDto.type` va `ProductDistributionDto.type` endi
 * enum SONI (eski modelda `string` deb yozilgan edi); `Top*` nomlari bo'sh
 * bo'lsa `null` keladi.
 */

export interface DashboardSummaryDto {
  readonly totalStockKg: number;
  readonly activeProductionOrders: number;
  readonly pendingTransfers: number;
  readonly monthlyRevenue: number;
  readonly revenueChangePercent: number;
  readonly lowStockProductCount: number;
  readonly overallEfficiencyPercent: number;
  readonly totalDebt: number;
}

export interface PlanVsActualDto {
  readonly date: string;
  readonly productName: string;
  readonly planned: number;
  readonly actual: number;
  readonly efficiencyPercent: number;
}

export interface DailyTransferDto {
  readonly date: string;
  readonly incomingTotal: number;
  readonly outgoingTotal: number;
  readonly incomingCount: number;
  readonly outgoingCount: number;
}

export interface StockLevelDto {
  readonly productName: string;
  readonly unitShortName: string;
  readonly currentStock: number;
  readonly minStock: number;
  readonly isLow: boolean;
}

export interface ProductDistributionDto {
  readonly productName: string;
  /** `ProductType` soni. */
  readonly type: number;
  readonly totalStock: number;
  readonly percentage: number;
}

export interface ExtendedDashboardSummaryDto {
  readonly totalIncome: number;
  readonly totalExpense: number;
  readonly netProfit: number;
  readonly totalDebt: number;
  readonly totalIncomingTransfers: number;
  readonly totalOutgoingTransfers: number;
  readonly totalIncomingAmount: number;
  readonly totalOutgoingAmount: number;
  readonly totalProductionOrders: number;
  readonly completedOrders: number;
  readonly totalProduced: number;
  readonly totalWaste: number;
  readonly totalStockValue: number;
  readonly lowStockCount: number;
  readonly topSellingProduct: string | null;
  readonly topDebtor: string | null;
  readonly topSupplier: string | null;
}

export interface MonthlyComparisonDto {
  readonly month: string;
  readonly income: number;
  readonly expense: number;
  readonly net: number;
}
