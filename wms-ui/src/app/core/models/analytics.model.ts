export interface DashboardSummaryDto {
  totalStockKg: number;
  activeProductionOrders: number;
  pendingTransfers: number;
  monthlyRevenue: number;
  revenueChangePercent: number;
  lowStockProductCount: number;
  overallEfficiencyPercent: number;
  totalDebt: number;
}

export interface PlanVsActualDto {
  date: string;
  productName: string;
  planned: number;
  actual: number;
  efficiencyPercent: number;
}

export interface DailyTransferDto {
  date: string;
  incomingTotal: number;
  outgoingTotal: number;
  incomingCount: number;
  outgoingCount: number;
}

export interface StockLevelDto {
  productName: string;
  unitShortName: string;
  currentStock: number;
  minStock: number;
  isLow: boolean;
}

export interface IncomeExpenseDto {
  date: string;
  income: number;
  expense: number;
  net: number;
}

export interface ShiftEfficiencyDto {
  shiftName: string;
  date: string;
  plannedQty: number;
  actualQty: number;
  efficiencyPercent: number;
}

export interface ProductDistributionDto {
  productName: string;
  type: number;
  totalStock: number;
  percentage: number;
}

export interface TopDebtorDto {
  counterpartyName: string;
  type: string;
  debtAmount: number;
}

export interface WasteByStageDto {
  stageName: string;
  totalActual: number;
  totalWaste: number;
  wastePercent: number;
}

export interface ExtendedDashboardSummaryDto {
  totalIncome: number;
  totalExpense: number;
  netProfit: number;
  totalDebt: number;
  totalIncomingTransfers: number;
  totalOutgoingTransfers: number;
  totalIncomingAmount: number;
  totalOutgoingAmount: number;
  totalProductionOrders: number;
  completedOrders: number;
  totalProduced: number;
  totalWaste: number;
  totalStockValue: number;
  lowStockCount: number;
  topSellingProduct: string;
  topDebtor: string;
  topSupplier: string;
}

export interface MonthlyComparisonDto {
  month: string;
  income: number;
  expense: number;
  net: number;
}
