import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { Textarea } from 'primeng/textarea';
import { Dialog } from 'primeng/dialog';
import { TranslocoDirective } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { ProductionService } from '../../../core/services/production.service';
import { NotificationService } from '../../../shared/services/notification.service';
import {
  ProductionOrder, ProductionOrderStatus,
  StageExecution, StageExecutionStatus,
  StageExecuteDto
} from '../../../core/models/production.model';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [
    DecimalPipe, DatePipe, FormsModule,
    TableModule, Button, InputNumber, Textarea, Dialog,
    PageHeaderComponent, StatusBadgeComponent, TranslocoDirective
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.scss'
})
export default class OrderDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private productionService = inject(ProductionService);
  private notify = inject(NotificationService);

  order = signal<ProductionOrder | null>(null);
  loading = signal(true);
  executing = signal(false);
  executeDialogVisible = signal(false);
  selectedStageId = signal<number | null>(null);

  executeForm = signal<{ actualQuantity: number; wasteQuantity: number; reworkQuantity: number; note: string | null }>({
    actualQuantity: 0,
    wasteQuantity: 0,
    reworkQuantity: 0,
    note: null
  });

  isDraft = computed(() => this.order()?.status === ProductionOrderStatus.Draft);
  isInProgress = computed(() => this.order()?.status === ProductionOrderStatus.InProgress);

  allStagesCompleted = computed(() => {
    const o = this.order();
    if (!o || !o.stageExecutions?.length) return false;
    return o.stageExecutions.every(s => s.status === StageExecutionStatus.Completed || s.status === StageExecutionStatus.Skipped);
  });

  sortedStages = computed(() => {
    const o = this.order();
    if (!o?.stageExecutions) return [];
    return [...o.stageExecutions].sort((a, b) => a.orderNumber - b.orderNumber);
  });

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) { this.router.navigate(['/production/orders']); return; }
    this.loadOrder(id);
  }

  loadOrder(id: number) {
    this.loading.set(true);
    this.productionService.getOrder(id).subscribe({
      next: (res) => {
        if (res.success && res.data) this.order.set(res.data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load production order');
      }
    });
  }

  startOrder() {
    const o = this.order();
    if (!o) return;
    this.notify.confirmAction('Start this production order?', 'Start Order', () => {
      this.productionService.startOrder(o.id).subscribe({
        next: () => {
          this.notify.success('Production order started');
          this.loadOrder(o.id);
        },
        // Xatoni error.interceptor ko'rsatadi (backend xabari)
        error: () => {}
      });
    });
  }

  completeOrder() {
    const o = this.order();
    if (!o) return;
    this.notify.confirmAction('Complete this production order? Output will be sent to warehouse.', 'Complete Order', () => {
      this.productionService.completeOrder(o.id).subscribe({
        next: () => {
          this.notify.success('Production order completed');
          this.loadOrder(o.id);
        },
        // Xatoni error.interceptor ko'rsatadi ("barcha bosqichlar tugashi kerak" va h.k.)
        error: () => {}
      });
    });
  }

  openExecuteDialog(stage: StageExecution) {
    this.selectedStageId.set(stage.id);
    this.executeForm.set({
      actualQuantity: stage.plannedQuantity,
      wasteQuantity: 0,
      reworkQuantity: 0,
      note: null
    });
    this.executeDialogVisible.set(true);
  }

  executeStage() {
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
      note: form.note
    };

    this.executing.set(true);
    this.productionService.executeStage(o.id, stageId, dto).subscribe({
      next: () => {
        this.executing.set(false);
        this.executeDialogVisible.set(false);
        this.notify.success('Stage executed successfully');
        this.loadOrder(o.id);
      },
      // Xatoni error.interceptor ko'rsatadi ("Insufficient stock..." va h.k.)
      error: () => {
        this.executing.set(false);
      }
    });
  }

  goBack() {
    this.router.navigate(['/production/orders']);
  }

  getOrderStatusKey(status: ProductionOrderStatus): string {
    switch (status) {
      case ProductionOrderStatus.Draft: return 'Draft';
      case ProductionOrderStatus.InProgress: return 'InProgress';
      case ProductionOrderStatus.Completed: return 'Completed';
      case ProductionOrderStatus.Cancelled: return 'Cancelled';
      default: return 'Neutral';
    }
  }

  getOrderStatusName(status: ProductionOrderStatus): string {
    switch (status) {
      case ProductionOrderStatus.Draft: return 'Draft';
      case ProductionOrderStatus.InProgress: return 'In Progress';
      case ProductionOrderStatus.Completed: return 'Completed';
      case ProductionOrderStatus.Cancelled: return 'Cancelled';
      default: return 'Unknown';
    }
  }

  getStageStatusKey(status: StageExecutionStatus): string {
    switch (status) {
      case StageExecutionStatus.Pending: return 'Pending';
      case StageExecutionStatus.InProgress: return 'InProgress';
      case StageExecutionStatus.Completed: return 'Completed';
      case StageExecutionStatus.Skipped: return 'Cancelled';
      default: return 'Neutral';
    }
  }

  getStageStatusName(status: StageExecutionStatus): string {
    switch (status) {
      case StageExecutionStatus.Pending: return 'Pending';
      case StageExecutionStatus.InProgress: return 'In Progress';
      case StageExecutionStatus.Completed: return 'Completed';
      case StageExecutionStatus.Skipped: return 'Skipped';
      default: return 'Unknown';
    }
  }

  getStageCardClass(status: StageExecutionStatus): string {
    switch (status) {
      case StageExecutionStatus.Completed: return 'stage-completed';
      case StageExecutionStatus.InProgress: return 'stage-in-progress';
      default: return 'stage-pending';
    }
  }

  canExecuteStage(stage: StageExecution): boolean {
    return this.isInProgress() &&
      (stage.status === StageExecutionStatus.Pending || stage.status === StageExecutionStatus.InProgress);
  }
}
