import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { AuditService } from '../../../core/services/audit.service';
import { AuditLog } from '../../../core/models/audit.model';
import { toLocalDateString, parseUtc } from '../../../shared/utils/date.util';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [DatePipe, FormsModule, TranslocoDirective, TableModule, Select, DatePicker, PageHeaderComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './audit-log.component.html',
  styleUrl: './audit-log.component.scss'
})
export default class AuditLogComponent implements OnInit {
  private auditService = inject(AuditService);

  logs = signal<AuditLog[]>([]);
  entityTypes = signal<{ label: string; value: string | null }[]>([{ label: '—', value: null }]);
  loading = signal(true);
  entityFilter = signal<string | null>(null);
  dateFrom = signal<Date | null>(null);
  dateTo = signal<Date | null>(null);

  ngOnInit() {
    this.loadEntityTypes();
    this.loadLogs();
  }

  loadLogs() {
    this.loading.set(true);
    const params: Record<string, string | number | boolean> = { page: 1, pageSize: 200 };
    if (this.entityFilter()) params['entityType'] = this.entityFilter()!;
    if (this.dateFrom()) params['from'] = toLocalDateString(this.dateFrom()!);
    if (this.dateTo()) params['to'] = toLocalDateString(this.dateTo()!);
    this.auditService.getLogs(params).subscribe({
      next: (res) => {
        this.logs.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  private loadEntityTypes() {
    this.auditService.getEntityTypes().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.entityTypes.set([
            { label: '—', value: null },
            ...res.data.map(t => ({ label: t, value: t }))
          ]);
        }
      }
    });
  }

  onFilterChange() { this.loadLogs(); }

  parse(dateStr: string): Date | null { return parseUtc(dateStr); }

  /** HTTP metodini StatusBadge CSS kalitiga aylantiradi. */
  actionClass(action: string): string {
    switch (action) {
      case 'POST': return 'Confirmed';   // yashil — yaratish
      case 'PUT': return 'Pending';      // sariq — o'zgartirish
      case 'PATCH': return 'Internal';   // neytral
      case 'DELETE': return 'Cancelled'; // qizil — o'chirish
      default: return 'Neutral';
    }
  }
}
