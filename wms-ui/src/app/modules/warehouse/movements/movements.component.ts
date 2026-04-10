import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { DatePicker } from 'primeng/datepicker';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { WarehouseService } from '../../../core/services/warehouse.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { StockMovement } from '../../../core/models/warehouse.model';

@Component({
  selector: 'app-movements',
  standalone: true,
  imports: [DecimalPipe, DatePipe, FormsModule, TranslocoDirective, TableModule, DatePicker, PageHeaderComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './movements.component.html',
  styleUrl: './movements.component.scss'
})
export default class MovementsComponent implements OnInit {
  private warehouseService = inject(WarehouseService);
  private notify = inject(NotificationService);

  movements = signal<StockMovement[]>([]);
  loading = signal(true);
  dateFrom = signal<Date | null>(null);
  dateTo = signal<Date | null>(null);

  ngOnInit() { this.loadMovements(); }

  loadMovements() {
    this.loading.set(true);
    const params: Record<string, string | number | boolean> = { status: 2 }; // Confirmed only
    if (this.dateFrom()) params['from'] = this.dateFrom()!.toISOString().split('T')[0];
    if (this.dateTo()) params['to'] = this.dateTo()!.toISOString().split('T')[0];
    this.warehouseService.getMovements(params).subscribe({
      next: (res) => {
        this.movements.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.notify.error('Failed to load movements'); }
    });
  }

  onDateChange() { this.loadMovements(); }

  getMovementStatus(type: string): string {
    return type === 'Incoming' ? 'Confirmed' : type === 'Outgoing' ? 'Cancelled' : 'InProgress';
  }
}
