import { ChangeDetectionStrategy, Component, type OnInit, computed, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { Textarea } from 'primeng/textarea';
import { Dialog } from 'primeng/dialog';
import { TranslocoDirective } from '@jsverse/transloco';

import { isWmsApiError } from '../../../core/api/wms-api-error';
import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import {
  ProductionOrderStatus,
  StageExecutionStatus,
  shortId,
  type ProductionOrder,
  type StageExecuteDto,
  type StageExecution,
} from '../production.model';
import { ProductionService } from '../production.service';
import { orderStatusClass, orderStatusKey, stageStatusClass, stageStatusKey } from '../production-status';
import { parseUtc } from '../../../core/utils/date.util';

interface ExecuteForm {
  readonly actualQuantity: number;
  readonly wasteQuantity: number;
  readonly reworkQuantity: number;
  readonly note: string | null;
}

@Component({
  selector: 'app-order-detail',
  imports: [
    DecimalPipe, DatePipe, FormsModule, Button, InputNumber, Textarea, Dialog,
    PageHeaderComponent, StatusBadgeComponent, TranslocoDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.scss',
})
export default class OrderDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly productionService = inject(ProductionService);
  private readonly notify = inject(NotificationService);

  protected readonly shortId = shortId;
  protected readonly orderStatusClass = orderStatusClass;
  protected readonly orderStatusKey = orderStatusKey;
  protected readonly stageStatusClass = stageStatusClass;
  protected readonly stageStatusKey = stageStatusKey;

  readonly order = signal<ProductionOrder | null>(null);
  readonly loading = signal(true);
  readonly executing = signal(false);
  readonly executeDialogVisible = signal(false);
  readonly selectedStageId = signal<string | null>(null);

  readonly executeForm = signal<ExecuteForm>({ actualQuantity: 0, wasteQuantity: 0, reworkQuantity: 0, note: null });

  readonly isDraft = computed(() => this.order()?.status === ProductionOrderStatus.Draft);
  readonly isInProgress = computed(() => this.order()?.status === ProductionOrderStatus.InProgress);

  readonly allStagesCompleted = computed(() => {
    const stages = this.order()?.stageExecutions ?? [];
    return (
      stages.length > 0 &&
      stages.every((s) => s.status === StageExecutionStatus.Completed || s.status === StageExecutionStatus.Skipped)
    );
  });

  readonly sortedStages = computed(() =>
    [...(this.order()?.stageExecutions ?? [])].sort((a, b) => a.orderNumber - b.orderNumber)
  );

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/production/orders']);
      return;
    }
    this.loadOrder(id);
  }

  loadOrder(id: string): void {
    this.loading.set(true);
    this.productionService.getOrder(id).subscribe({
      next: (res) => {
        if (res.success && res.data) this.order.set(res.data);
        this.loading.set(false);
      },
      // Xato toastini qobiq chiqaradi.
      error: () => this.loading.set(false),
    });
  }

  startOrder(): void {
    const o = this.order();
    if (!o) return;
    this.notify.confirmAction('Start this production order?', 'Start Order', () => {
      this.productionService.startOrder(o.id).subscribe({
        next: () => {
          this.notify.success('Production order started');
          this.loadOrder(o.id);
        },
        error: (err: unknown) => this.reloadOnConflict(err, o.id),
      });
    });
  }

  completeOrder(): void {
    const o = this.order();
    if (!o) return;
    this.notify.confirmAction('Complete this production order? Output will be sent to warehouse.', 'Complete Order', () => {
      this.productionService.completeOrder(o.id).subscribe({
        next: () => {
          this.notify.success('Production order completed');
          this.loadOrder(o.id);
        },
        error: (err: unknown) => this.reloadOnConflict(err, o.id),
      });
    });
  }

  openExecuteDialog(stage: StageExecution): void {
    this.selectedStageId.set(stage.id);
    this.executeForm.set({ actualQuantity: stage.plannedQuantity, wasteQuantity: 0, reworkQuantity: 0, note: null });
    this.executeDialogVisible.set(true);
  }

  executeStage(): void {
    const o = this.order();
    const stageId = this.selectedStageId();
    const form = this.executeForm();
    if (!o || !stageId) return;

    if (!form.actualQuantity || form.actualQuantity <= 0) {
      this.notify.warn('Actual quantity must be greater than 0');
      return;
    }

    const dto: StageExecuteDto = {
      actualQuantity: form.actualQuantity,
      wasteQuantity: form.wasteQuantity,
      reworkQuantity: form.reworkQuantity,
      note: form.note,
    };

    this.executing.set(true);
    this.productionService.executeStage(o.id, stageId, dto).subscribe({
      next: () => {
        this.executing.set(false);
        this.executeDialogVisible.set(false);
        this.notify.success('Stage executed successfully');
        this.loadOrder(o.id);
      },
      // Xatoni qobiq ko'rsatadi ("Insufficient stock..." va h.k.).
      error: (err: unknown) => {
        this.executing.set(false);
        if (this.reloadOnConflict(err, o.id)) this.executeDialogVisible.set(false);
      },
    });
  }

  updateExecuteForm<K extends keyof ExecuteForm>(field: K, value: ExecuteForm[K]): void {
    this.executeForm.update((f) => ({ ...f, [field]: value }));
  }

  goBack(): void {
    void this.router.navigate(['/production/orders']);
  }

  stageCardClass(status: StageExecutionStatus): string {
    switch (status) {
      case StageExecutionStatus.Completed:
        return 'stage-completed';
      case StageExecutionStatus.InProgress:
        return 'stage-in-progress';
      default:
        return 'stage-pending';
    }
  }

  canExecuteStage(stage: StageExecution): boolean {
    return (
      this.isInProgress() &&
      (stage.status === StageExecutionStatus.Pending || stage.status === StageExecutionStatus.InProgress)
    );
  }

  /**
   * F6 (D13): parallel ikki yakunlash/boshlashda ikkinchisi 409 oladi — toastni qobiq
   * chiqaradi, lekin ekrandagi buyurtma endi ESKI (boshqa odam holatini o'zgartirgan).
   * Qayta o'qilmasa foydalanuvchi yana o'sha tugmani bosib, yana 409 olardi.
   */
  private reloadOnConflict(err: unknown, id: string): boolean {
    if (isWmsApiError(err) && err.status === 409) {
      this.loadOrder(id);
      return true;
    }
    return false;
  }

  /**
   * Serverdagi vaqt UTC'da keladi va `Z` siz kelishi mumkin — `new Date` uni
   * mahalliy deb o'qib 5 soat siljitardi (`core/utils/date.util`).
   */
  parse(value: string | null | undefined): Date | null {
    return parseUtc(value);
  }

}
