import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal, type OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { InputText } from 'primeng/inputtext';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { DatePicker } from 'primeng/datepicker';
import { Textarea } from 'primeng/textarea';
import { LanguageService } from '@agentics/i18n';

import { NotificationService } from '../../../core/notify/notification.service';
import { toLocalDateString } from '../../../core/utils/date.util';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import type { Batch, UpdateBatchDto } from '../warehouse.model';
import { WarehouseService } from '../warehouse.service';

interface BatchEditForm {
  readonly id: string;
  readonly productName: string;
  readonly lotNumber: string;
  readonly expiryDate: Date | null;
  readonly notes: string | null;
  readonly initialQuantity: number;
  readonly remainingQuantity: number;
}

/** «Muddati yaqin» chegarasi — eski ekran bilan bir xil. */
const EXPIRING_SOON_DAYS = 30;
const DAY_MS = 1000 * 60 * 60 * 24;

/** Partiyalar va yaroqlilik muddati (eski `warehouse/batches`). */
@Component({
  selector: 'app-batches',
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
    HasPermissionDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './batches.component.html',
  styleUrl: './batches.component.scss',
})
export default class BatchesComponent implements OnInit {
  private readonly warehouseService = inject(WarehouseService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  readonly batches = signal<Batch[]>([]);
  readonly loading = signal(true);
  readonly search = signal('');
  readonly filtered = computed(() => {
    const q = this.search().toLowerCase();
    return q
      ? this.batches().filter((b) => b.productName.toLowerCase().includes(q) || b.lotNumber.toLowerCase().includes(q))
      : this.batches();
  });

  readonly dialogVisible = signal(false);
  readonly saving = signal(false);
  readonly editForm = signal<BatchEditForm | null>(null);

  /** Bildirishnomadan (`?highlight=<id>`) kirilganda qator bir necha soniya yonadi. */
  readonly highlightId = signal<string | null>(null);
  private highlightTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.loadBatches();
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const id = params.get('highlight');
      if (!id) return;
      this.highlightId.set(id);
      this.clearHighlightTimer();
      this.highlightTimer = setTimeout(() => {
        this.highlightId.set(null);
        this.highlightTimer = null;
      }, 3500);
    });
    // Sahifadan chiqilganda taymer yopilgan komponent signaliga yozmasin.
    this.destroyRef.onDestroy(() => this.clearHighlightTimer());
  }

  loadBatches(): void {
    this.loading.set(true);
    this.warehouseService.getBatches().subscribe({
      next: (res) => {
        this.batches.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  expiryStatus(date: string | null): string {
    const days = daysUntil(date);
    if (days === null) return 'Active';
    if (days < 0) return 'Cancelled';
    return days <= EXPIRING_SOON_DAYS ? 'Pending' : 'Active';
  }

  expiryKey(date: string | null): string {
    const days = daysUntil(date);
    if (days === null) return 'status.noExpiry';
    if (days < 0) return 'status.expired';
    return days <= EXPIRING_SOON_DAYS ? 'status.expiringSoon' : 'status.ok';
  }

  openEdit(b: Batch): void {
    this.editForm.set({
      id: b.id,
      productName: b.productName,
      lotNumber: b.lotNumber,
      expiryDate: b.expiryDate ? new Date(b.expiryDate) : null,
      notes: b.notes,
      initialQuantity: b.initialQuantity,
      remainingQuantity: b.remainingQuantity,
    });
    this.dialogVisible.set(true);
  }

  updateForm<K extends keyof BatchEditForm>(field: K, value: BatchEditForm[K]): void {
    this.editForm.update((f) => (f ? { ...f, [field]: value } : f));
  }

  saveEdit(): void {
    const f = this.editForm();
    if (!f) return;
    if (!f.lotNumber.trim()) {
      this.notify.warn(this.language.translate('batch.lotRequired'));
      return;
    }
    const dto: UpdateBatchDto = {
      lotNumber: f.lotNumber.trim(),
      // Yaroqlilik muddati — kalendar kuni, vaqt nuqtasi emas: mahalliy `YYYY-MM-DD`.
      expiryDate: f.expiryDate ? toLocalDateString(f.expiryDate) : null,
      notes: f.notes?.trim() ? f.notes.trim() : null,
    };
    this.saving.set(true);
    this.warehouseService.updateBatch(f.id, dto).subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.language.translate('batch.updatedSuccess'));
        this.loadBatches();
      },
      // Xato toastini (server sababi bilan) `WmsErrorNotifier` allaqachon chiqardi.
      error: () => this.saving.set(false),
    });
  }

  deleteBatch(b: Batch): void {
    this.notify.confirmDelete(this.language.translate('batch.deleteConfirm'), () => {
      this.warehouseService.deleteBatch(b.id).subscribe({
        next: () => {
          this.notify.success(this.language.translate('batch.deletedSuccess'));
          this.loadBatches();
        },
        // Qoldiqli partiya — server 4xx sababi bilan qaytaradi; toast `WmsErrorNotifier` da.
        error: () => undefined,
      });
    });
  }

  private clearHighlightTimer(): void {
    if (this.highlightTimer !== null) {
      clearTimeout(this.highlightTimer);
      this.highlightTimer = null;
    }
  }
}

function daysUntil(date: string | null): number | null {
  if (!date) return null;
  return (new Date(date).getTime() - Date.now()) / DAY_MS;
}
