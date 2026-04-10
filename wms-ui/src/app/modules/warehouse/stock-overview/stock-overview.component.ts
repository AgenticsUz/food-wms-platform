import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Select } from 'primeng/select';
import { InputText } from 'primeng/inputtext';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { WarehouseService } from '../../../core/services/warehouse.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { Warehouse, WarehouseStockRow } from '../../../core/models/warehouse.model';

@Component({
  selector: 'app-stock-overview',
  standalone: true,
  imports: [DecimalPipe, FormsModule, TranslocoDirective, TableModule, Select, InputText, PageHeaderComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './stock-overview.component.html',
  styleUrl: './stock-overview.component.scss'
})
export default class StockOverviewComponent implements OnInit {
  private warehouseService = inject(WarehouseService);
  private notify = inject(NotificationService);

  warehouses = signal<Warehouse[]>([]);
  selectedWarehouseId = signal<number | null>(null);
  stock = signal<WarehouseStockRow[]>([]);
  filtered = signal<WarehouseStockRow[]>([]);
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

    if (whId) {
      // Load stock for a specific warehouse
      const wh = this.warehouses().find(w => w.id === whId);
      if (!wh) { this.loading.set(false); return; }
      this.warehouseService.getStockRows(wh).subscribe({
        next: (rows) => {
          this.stock.set(rows);
          this.applyFilter();
          this.loading.set(false);
        },
        error: () => { this.loading.set(false); this.notify.error('Failed to load stock'); }
      });
    } else {
      // Load stock from ALL warehouses
      this.warehouseService.getAllStockRows(this.warehouses()).subscribe({
        next: (rows) => {
          this.stock.set(rows);
          this.applyFilter();
          this.loading.set(false);
        },
        error: () => { this.loading.set(false); this.notify.error('Failed to load stock'); }
      });
    }
  }

  applyFilter() {
    const q = this.search().toLowerCase();
    this.filtered.set(q
      ? this.stock().filter(s => s.productName.toLowerCase().includes(q) || s.warehouseName.toLowerCase().includes(q))
      : this.stock());
  }

  onSearch(value: string) { this.search.set(value); this.applyFilter(); }
  onWarehouseChange(value: number | null) { this.selectedWarehouseId.set(value); this.loadStock(); }

  isLow(s: WarehouseStockRow): boolean {
    return s.availableQuantity <= 0;
  }
}
