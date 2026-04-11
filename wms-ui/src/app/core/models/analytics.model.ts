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
  productType: string;
  count: number;
  percentage: number;
}

export interface RecentTransferDto {
  id: number;
  type: string;
  status: string;
  counterpartyName: string;
  totalAmount: number;
  itemCount: number;
  createdAt: string;
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
