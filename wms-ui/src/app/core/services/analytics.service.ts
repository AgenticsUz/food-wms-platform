import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import {
  DashboardSummaryDto,
  PlanVsActualDto,
  DailyTransferDto,
  ProductDistributionDto,
  RecentTransferDto,
  StockLevelDto,
  TopDebtorDto,
  WasteByStageDto,
  IncomeExpenseDto,
  ShiftEfficiencyDto,
  ExtendedDashboardSummaryDto,
  MonthlyComparisonDto
} from '../models/analytics.model';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private api = inject(ApiService);

  getSummary() {
    return this.api.get<DashboardSummaryDto>('analytics/summary');
  }

  getProductionPlanVsActual(days = 7) {
    return this.api.get<PlanVsActualDto[]>('analytics/production/plan-vs-actual', { days });
  }

  getDailyTransfers(days = 7) {
    return this.api.get<DailyTransferDto[]>('analytics/transfers/daily', { days });
  }

  getStockLevels(warehouseId?: number) {
    return this.api.get<StockLevelDto[]>('analytics/warehouse/stock-levels',
      warehouseId ? { warehouseId } : {});
  }

  getProductDistribution() {
    return this.api.get<ProductDistributionDto[]>('analytics/products/distribution');
  }

  getRecentTransfers(count = 5) {
    return this.api.get<RecentTransferDto[]>('transfers', { page: 1, pageSize: count });
  }

  getTopDebtors(top = 5) {
    return this.api.get<TopDebtorDto[]>('analytics/finance/top-debtors', { top });
  }

  getWasteByStage(days = 30) {
    return this.api.get<WasteByStageDto[]>('analytics/production/waste-by-stage', { days });
  }

  getIncomeExpense(days = 30) {
    return this.api.get<IncomeExpenseDto[]>('analytics/finance/income-expense', { days });
  }

  getShiftEfficiency(days = 7) {
    return this.api.get<ShiftEfficiencyDto[]>('analytics/kpi/shift-efficiency', { days });
  }

  getExtendedSummary(fromDate?: string, toDate?: string) {
    const params: Record<string, string> = {};
    if (fromDate) params['fromDate'] = fromDate;
    if (toDate) params['toDate'] = toDate;
    return this.api.get<ExtendedDashboardSummaryDto>('analytics/dashboard-summary', params);
  }

  getMonthlyComparison() {
    return this.api.get<MonthlyComparisonDto[]>('analytics/monthly-comparison');
  }
}
