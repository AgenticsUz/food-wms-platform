import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { InputNumber } from 'primeng/inputnumber';
import { TranslocoDirective } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { ProductionService } from '../../../core/services/production.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { ProductionStage, ProductionStageCreateDto } from '../../../core/models/production.model';

@Component({
  selector: 'app-stages',
  standalone: true,
  imports: [FormsModule, TableModule, Button, InputText, Dialog, InputNumber, PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stages.component.html',
  styleUrl: './stages.component.scss'
})
export default class StagesComponent implements OnInit {
  private productionService = inject(ProductionService);
  private notify = inject(NotificationService);

  stages = signal<ProductionStage[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  editing = signal(false);
  saving = signal(false);

  form = signal<ProductionStageCreateDto & { id?: number }>({
    name: '',
    orderNumber: 1,
    description: null
  });

  ngOnInit() {
    this.loadStages();
  }

  loadStages() {
    this.loading.set(true);
    this.productionService.getStages().subscribe({
      next: (res) => {
        const list = res.success && res.data ? res.data : [];
        this.stages.set(list.sort((a, b) => a.orderNumber - b.orderNumber));
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); }
    });
  }

  openNew() {
    const maxOrder = this.stages().reduce((max, s) => Math.max(max, s.orderNumber), 0);
    this.form.set({ name: '', orderNumber: maxOrder + 1, description: null });
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(stage: ProductionStage) {
    this.form.set({
      id: stage.id,
      name: stage.name,
      orderNumber: stage.orderNumber,
      description: stage.description
    });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.name.trim()) { this.notify.warn('Stage name is required'); return; }
    this.saving.set(true);
    const dto: ProductionStageCreateDto = {
      name: f.name.trim(),
      orderNumber: f.orderNumber,
      description: f.description
    };
    const obs = this.editing()
      ? this.productionService.updateStage(f.id!, dto)
      : this.productionService.createStage(dto);
    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.editing() ? 'Stage updated' : 'Stage created');
        this.loadStages();
      },
      error: () => { this.saving.set(false); }
    });
  }

  deleteStage(stage: ProductionStage) {
    this.notify.confirmDelete(`Delete "${stage.name}"?`, () => {
      this.productionService.deleteStage(stage.id).subscribe({
        next: () => { this.notify.success('Stage deleted'); this.loadStages(); },
        error: () => this.notify.error('Failed to delete stage')
      });
    });
  }

  moveUp(stage: ProductionStage) {
    const list = this.stages();
    const idx = list.findIndex(s => s.id === stage.id);
    if (idx <= 0) return;
    const reordered = [...list];
    [reordered[idx - 1], reordered[idx]] = [reordered[idx], reordered[idx - 1]];
    this.stages.set(reordered);
    this.reorder(reordered);
  }

  moveDown(stage: ProductionStage) {
    const list = this.stages();
    const idx = list.findIndex(s => s.id === stage.id);
    if (idx < 0 || idx >= list.length - 1) return;
    const reordered = [...list];
    [reordered[idx], reordered[idx + 1]] = [reordered[idx + 1], reordered[idx]];
    this.stages.set(reordered);
    this.reorder(reordered);
  }

  private reorder(list: ProductionStage[]) {
    const ids = list.map(s => s.id);
    this.productionService.reorderStages(ids).subscribe({
      next: () => this.notify.success('Order updated'),
      error: () => { this.loadStages(); }
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

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }
}
