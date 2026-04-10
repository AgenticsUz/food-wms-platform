import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Select } from 'primeng/select';
import { InputText } from 'primeng/inputtext';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { WarehouseService } from '../../../core/services/warehouse.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Warehouse, WarehouseStock } from '../../../core/models/warehouse.model';

@Component({
  selector: 'app-stock-overview',
  standalone: true,
  imports: [DecimalPipe, DatePipe, FormsModule, TableModule, Select, InputText, PageHeaderComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stock-overview.component.html',
  styleUrl: './stock-overview.component.scss'
})
export default class StockOverviewComponent implements OnInit {
  private warehouseService = inject(WarehouseService);
  private notify = inject(NotificationService);

  warehouses = signal<Warehouse[]>([]);
  selectedWarehouseId = signal<number | null>(null);
  stock = signal<WarehouseStock[]>([]);
  filtered = signal<WarehouseStock[]>([]);
  loading = signal(true);
  search = signal('');

  warehouseOptions = signal<{ label: string; value: number | null }[]>([]);

  ngOnInit() {
    this.loadWarehouses();
  }

  private loadWarehouses() {
    this.warehouseService.getWarehouses().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.warehouses.set(res.data);
          this.warehouseOptions.set([
            { label: 'All Warehouses', value: null },
            ...res.data.map(w => ({ label: w.name, value: w.id }))
          ]);
        }
        this.loadStock();
      },
      error: () => { this.loading.set(false); this.notify.error('Failed to load warehouses'); }
    });
  }

  loadStock() {
    this.loading.set(true);
    const whId = this.selectedWarehouseId();
    const obs = whId
      ? this.warehouseService.getStock(whId)
      : this.warehouseService.getAllStock();
    obs.subscribe({
      next: (res) => {
        const list = res.success && res.data ? res.data : [];
        this.stock.set(list);
        this.applyFilter();
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.notify.error('Failed to load stock'); }
    });
  }

  applyFilter() {
    const q = this.search().toLowerCase();
    this.filtered.set(q
      ? this.stock().filter(s => s.productName.toLowerCase().includes(q) || s.lotNumber.toLowerCase().includes(q))
      : this.stock());
  }

  onSearch(value: string) { this.search.set(value); this.applyFilter(); }
  onWarehouseChange(value: number | null) { this.selectedWarehouseId.set(value); this.loadStock(); }

  isExpired(date: string | null): boolean {
    if (!date) return false;
    return new Date(date) < new Date();
  }

  isExpiringSoon(date: string | null): boolean {
    if (!date) return false;
    const d = new Date(date);
    const now = new Date();
    const diff = (d.getTime() - now.getTime()) / (1000 * 60 * 60 * 24);
    return diff > 0 && diff <= 30;
  }

  getExpiryStatus(date: string | null): string {
    if (!date) return 'Active';
    if (this.isExpired(date)) return 'Cancelled';
    if (this.isExpiringSoon(date)) return 'Pending';
    return 'Active';
  }

  getExpiryLabel(date: string | null): string {
    if (!date) return 'No expiry';
    if (this.isExpired(date)) return 'Expired';
    if (this.isExpiringSoon(date)) return 'Expiring';
    return 'OK';
  }

  isLow(s: WarehouseStock): boolean {
    return s.quantity <= 0;
  }
}
