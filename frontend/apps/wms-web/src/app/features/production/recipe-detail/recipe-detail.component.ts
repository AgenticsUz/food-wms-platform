import { ChangeDetectionStrategy, Component, type OnInit, computed, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { TranslocoDirective } from '@jsverse/transloco';

import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import type { ProductionRecipe, WarehouseOption } from '../production.model';
import { ProductionService } from '../production.service';

@Component({
  selector: 'app-recipe-detail',
  imports: [DecimalPipe, TableModule, Button, PageHeaderComponent, StatusBadgeComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './recipe-detail.component.html',
  styleUrl: './recipe-detail.component.scss',
})
export default class RecipeDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly productionService = inject(ProductionService);

  readonly recipe = signal<ProductionRecipe | null>(null);
  readonly loading = signal(true);
  private readonly warehouses = signal<WarehouseOption[]>([]);

  readonly sortedStages = computed(() =>
    [...(this.recipe()?.stages ?? [])].sort((a, b) => a.orderNumber - b.orderNumber)
  );

  /**
   * F6 `RecipeStageDto` da `outputWarehouseName` YO'Q (faqat id) — eski ekrandagi
   * ombor yorlig'i saqlanishi uchun nom omborlar ro'yxatidan topiladi. Ro'yxat
   * o'qilmasa (ruxsat yo'q) yorliq shunchaki chiqmaydi.
   */
  private readonly warehouseNames = computed(
    () => new Map(this.warehouses().map((w) => [w.id, w.name] as const))
  );

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/production/recipes']);
      return;
    }
    this.productionService.getRecipe(id).subscribe({
      next: (res) => {
        this.recipe.set(res.data ?? null);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
    this.productionService.getWarehouses().subscribe({
      next: (res) => this.warehouses.set(res.data ?? []),
      error: () => undefined,
    });
  }

  warehouseName(id: string | null): string | null {
    return id === null ? null : (this.warehouseNames().get(id) ?? null);
  }

  goBack(): void {
    void this.router.navigate(['/production/recipes']);
  }
}
