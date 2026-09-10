import { Injectable, inject } from '@angular/core';

import { ApiService } from '../../core/api/api.service';
import type { Transfer } from '../transfers/transfer.model';
import type {
  DailyTransferDto,
  DashboardSummaryDto,
  ExtendedDashboardSummaryDto,
  MonthlyComparisonDto,
  PlanVsActualDto,
  ProductDistributionDto,
  StockLevelDto,
} from './analytics.model';

/**
 * Bosh sahifa va zaxira grafigi analitikasi (eski `core/services/analytics.service`
 * ning shu ekranlar ishlatadigan qismi; KPI/moliya grafiklari o'z bo'limlarida).
 */
@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly api = inject(ApiService);

  getSummary() {
    return this.api.get<DashboardSummaryDto>('analytics/summary');
  }

  getProductionPlanVsActual(days = 7) {
    return this.api.get<PlanVsActualDto[]>('analytics/production/plan-vs-actual', { days });
  }

  getDailyTransfers(days = 7) {
    return this.api.get<DailyTransferDto[]>('analytics/transfers/daily', { days });
  }

  getStockLevels(warehouseId?: string) {
    return this.api.get<StockLevelDto[]>('analytics/warehouse/stock-levels', { warehouseId });
  }

  getProductDistribution() {
    return this.api.get<ProductDistributionDto[]>('analytics/products/distribution');
  }

  /** `/transfers` to'liq `TransferDto` qaytaradi — alohida «so'nggi» endpoint yo'q. */
  getRecentTransfers(count = 5) {
    return this.api.get<Transfer[]>('transfers', { page: 1, pageSize: count });
  }

  /** `fromDate`/`toDate` — UTC vaqt nuqtalari (`localDayRangeToUtc`); berilmasa oy boshidan hozirgacha. */
  getExtendedSummary(fromDate?: string, toDate?: string) {
    return this.api.get<ExtendedDashboardSummaryDto>('analytics/dashboard-summary', { fromDate, toDate });
  }

  getMonthlyComparison() {
    return this.api.get<MonthlyComparisonDto[]>('analytics/monthly-comparison');
  }
}
