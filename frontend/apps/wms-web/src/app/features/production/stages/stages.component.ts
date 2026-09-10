import { ChangeDetectionStrategy, Component, type OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { TranslocoDirective } from '@jsverse/transloco';

import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type { ProductionStage, ProductionStageSaveDto } from '../production.model';
import { ProductionService } from '../production.service';

interface StageForm {
  readonly id: string | null;
  readonly name: string;
  readonly orderNumber: number;
  readonly description: string | null;
}

@Component({
  selector: 'app-stages',
  imports: [FormsModule, TableModule, Button, InputText, Dialog, InputNumber, PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stages.component.html',
  styleUrl: './stages.component.scss',
})
export default class StagesComponent implements OnInit {
  private readonly productionService = inject(ProductionService);
  private readonly notify = inject(NotificationService);

  readonly stages = signal<ProductionStage[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly editing = signal(false);
  readonly saving = signal(false);

  readonly form = signal<StageForm>({ id: null, name: '', orderNumber: 1, description: null });

  ngOnInit(): void {
    this.loadStages();
  }

  loadStages(): void {
    this.loading.set(true);
    this.productionService.getStages().subscribe({
      next: (res) => {
        const list = res.success && res.data ? [...res.data] : [];
        this.stages.set(list.sort((a, b) => a.orderNumber - b.orderNumber));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openNew(): void {
    const maxOrder = this.stages().reduce((max, s) => Math.max(max, s.orderNumber), 0);
    this.form.set({ id: null, name: '', orderNumber: maxOrder + 1, description: null });
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(stage: ProductionStage): void {
    this.form.set({
      id: stage.id,
      name: stage.name,
      orderNumber: stage.orderNumber,
      description: stage.description,
    });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save(): void {
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn('Stage name is required');
      return;
    }
    this.saving.set(true);
    const dto: ProductionStageSaveDto = {
      name: f.name.trim(),
      orderNumber: f.orderNumber,
      description: f.description,
    };
    const request =
      this.editing() && f.id !== null
        ? this.productionService.updateStage(f.id, dto)
        : this.productionService.createStage(dto);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.editing() ? 'Stage updated' : 'Stage created');
        this.loadStages();
      },
      error: () => this.saving.set(false),
    });
  }

  deleteStage(stage: ProductionStage): void {
    this.notify.confirmDelete(`Delete "${stage.name}"?`, () => {
      this.productionService.deleteStage(stage.id).subscribe({
        next: () => {
          this.notify.success('Stage deleted');
          this.loadStages();
        },
        // Xato toastini qobiq (`WmsErrorNotifier`) chiqaradi — ikkinchisi kerak emas.
        error: () => undefined,
      });
    });
  }

  moveUp(stage: ProductionStage): void {
    const list = this.stages();
    const idx = list.findIndex((s) => s.id === stage.id);
    if (idx <= 0) return;
    const reordered = [...list];
    [reordered[idx - 1], reordered[idx]] = [reordered[idx], reordered[idx - 1]];
    this.stages.set(reordered);
    this.reorder(reordered);
  }

  moveDown(stage: ProductionStage): void {
    const list = this.stages();
    const idx = list.findIndex((s) => s.id === stage.id);
    if (idx < 0 || idx >= list.length - 1) return;
    const reordered = [...list];
    [reordered[idx], reordered[idx + 1]] = [reordered[idx + 1], reordered[idx]];
    this.stages.set(reordered);
    this.reorder(reordered);
  }

  private reorder(list: readonly ProductionStage[]): void {
    this.productionService.reorderStages(list.map((s) => s.id)).subscribe({
      next: () => this.notify.success('Order updated'),
      // Server rad etsa, optimistik almashtirilgan tartib qaytariladi.
      error: () => this.loadStages(),
    });
  }

  isFirst(stage: ProductionStage): boolean {
    const list = this.stages();
    return list.length > 0 && list[0].id === stage.id;
  }

  isLast(stage: ProductionStage): boolean {
    const list = this.stages();
    return list.length > 0 && list[list.length - 1].id === stage.id;
  }

  updateForm<K extends keyof StageForm>(field: K, value: StageForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }
}
