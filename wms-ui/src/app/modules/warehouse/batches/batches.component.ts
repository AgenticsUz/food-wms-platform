import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { toLocalDateString } from '../../../shared/utils/date.util';
import { DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { InputText } from 'primeng/inputtext';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { DatePicker } from 'primeng/datepicker';
import { Textarea } from 'primeng/textarea';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { WarehouseService } from '../../../core/services/warehouse.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Batch, UpdateBatchDto } from '../../../core/models/warehouse.model';

interface BatchEditForm {
  id: number;
  productName: string;
  lotNumber: string;
  expiryDate: Date | null;
  notes: string | null;
  initialQuantity: number;
  remainingQuantity: number;
}

@Component({
  selector: 'app-batches',
  standalone: true,
  imports: [
    DecimalPipe,
    DatePipe,
    FormsModule,
    TranslocoDirective,
    TableModule,
    InputText,
    Button,
    Dialog,
    DatePicker,
    Textarea,
    PageHeaderComponent,
    StatusBadgeComponent,
    HasPermissionDirective
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './batches.component.html',
  styleUrl: './batches.component.scss'
})
export default class BatchesComponent implements OnInit {
  private warehouseService = inject(WarehouseService);
  private notify = inject(NotificationService);
  private transloco = inject(TranslocoService);
  private route = inject(ActivatedRoute);

  batches = signal<Batch[]>([]);
  filtered = signal<Batch[]>([]);
  loading = signal(true);
  search = signal('');

  // Edit dialog
  dialogVisible = signal(false);
  saving = signal(false);
  editForm = signal<BatchEditForm | null>(null);

  // Highlight (notification orqali kirilganda)
  highlightId = signal<number | null>(null);
  private highlightTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit() {
    this.loadBatches();
    this.route.queryParams.subscribe(params => {
      const id = params['highlight'];
      if (id) {
        const numId = Number(id);
        this.highlightId.set(numId);
        // Mavjud timer bo'lsa tozalaymiz
        if (this.highlightTimer) clearTimeout(this.highlightTimer);
        this.highlightTimer = setTimeout(() => {
          this.highlightId.set(null);
          this.highlightTimer = null;
        }, 3500);
      }
    });
  }

  loadBatches() {
    this.loading.set(true);
    this.warehouseService.getBatches().subscribe({
      next: (res) => {
        const list = res.success && res.data ? res.data : [];
        this.batches.set(list);
        this.applyFilter();
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.notify.error('Failed to load batches'); }
    });
  }

  applyFilter() {
    const q = this.search().toLowerCase();
    this.filtered.set(q
      ? this.batches().filter(b => b.productName.toLowerCase().includes(q) || b.lotNumber.toLowerCase().includes(q))
      : this.batches());
  }

  onSearch(value: string) { this.search.set(value); this.applyFilter(); }

  getExpiryStatus(date: string | null): string {
    if (!date) return 'Active';
    const d = new Date(date);
    const now = new Date();
    if (d < now) return 'Cancelled';
    const diff = (d.getTime() - now.getTime()) / (1000 * 60 * 60 * 24);
    if (diff <= 30) return 'Pending';
    return 'Active';
  }

  getExpiryLabel(date: string | null): string {
    if (!date) return 'No expiry';
    const d = new Date(date);
    const now = new Date();
    if (d < now) return 'Expired';
    const diff = (d.getTime() - now.getTime()) / (1000 * 60 * 60 * 24);
    if (diff <= 30) return 'Expiring soon';
    return 'OK';
  }

  // ---- Edit ----
  openEdit(b: Batch) {
    this.editForm.set({
      id: b.id,
      productName: b.productName,
      lotNumber: b.lotNumber,
      expiryDate: b.expiryDate ? new Date(b.expiryDate) : null,
      notes: b.notes ?? null,
      initialQuantity: b.initialQuantity,
      remainingQuantity: b.remainingQuantity
    });
    this.dialogVisible.set(true);
  }

  updateForm<K extends keyof BatchEditForm>(field: K, value: BatchEditForm[K]) {
    this.editForm.update(f => f ? ({ ...f, [field]: value }) : f);
  }

  saveEdit() {
    const f = this.editForm();
    if (!f) return;
    if (!f.lotNumber.trim()) {
      this.notify.warn(this.transloco.translate('batch.lotRequired'));
      return;
    }
    const dto: UpdateBatchDto = {
      lotNumber: f.lotNumber.trim(),
      expiryDate: f.expiryDate ? toLocalDateString(f.expiryDate) : null,
      notes: f.notes?.trim() ? f.notes.trim() : null
    };
    this.saving.set(true);
    this.warehouseService.updateBatch(f.id, dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.transloco.translate('batch.updatedSuccess'));
        this.loadBatches();
      },
      error: (err) => {
        this.saving.set(false);
        this.notify.error(err?.error?.message ?? 'Failed to update batch');
      }
    });
  }

  // ---- Delete ----
  deleteBatch(b: Batch) {
    this.notify.confirmDelete(this.transloco.translate('batch.deleteConfirm'), () => {
      this.warehouseService.deleteBatch(b.id).subscribe({
        next: () => {
          this.notify.success(this.transloco.translate('batch.deletedSuccess'));
          this.loadBatches();
        },
        error: (err) => {
          const msg = err?.error?.message ?? this.transloco.translate('batch.cannotDelete');
          this.notify.error(msg);
        }
      });
    });
  }
}
