import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { InputText } from 'primeng/inputtext';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { WarehouseService } from '../../../core/services/warehouse.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Batch } from '../../../core/models/warehouse.model';

@Component({
  selector: 'app-batches',
  standalone: true,
  imports: [DecimalPipe, DatePipe, FormsModule, TableModule, InputText, PageHeaderComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './batches.component.html',
  styleUrl: './batches.component.scss'
})
export default class BatchesComponent implements OnInit {
  private warehouseService = inject(WarehouseService);
  private notify = inject(NotificationService);

  batches = signal<Batch[]>([]);
  filtered = signal<Batch[]>([]);
  loading = signal(true);
  search = signal('');

  ngOnInit() { this.loadBatches(); }

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
}
