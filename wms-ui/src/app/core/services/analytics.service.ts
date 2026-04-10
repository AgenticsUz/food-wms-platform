import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import {
  DashboardSummaryDto,
  PlanVsActualDto,
  DailyTransferDto,
  ProductDistributionDto,
  RecentTransferDto,
  StockLevelDto
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
}
