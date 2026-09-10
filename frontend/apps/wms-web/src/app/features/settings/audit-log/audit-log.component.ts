import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { DatePicker } from 'primeng/datepicker';
import { Select } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { ToggleSwitch } from 'primeng/toggleswitch';

import { localDayRangeToUtc, parseUtc } from '../../../core/utils/date.util';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import type { AuditLog } from '../settings.model';
import { SettingsService } from '../settings.service';

/** Bir sahifada ko'rsatiladigan yozuvlar — «faqat platforma» filtri shu to'plam ustida. */
const PAGE_SIZE = 200;

@Component({
  selector: 'app-audit-log',
  imports: [
    DatePipe,
    FormsModule,
    TranslocoDirective,
    TableModule,
    Select,
    ToggleSwitch,
    DatePicker,
    PageHeaderComponent,
    StatusBadgeComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './audit-log.component.html',
  styleUrl: './audit-log.component.scss',
})
export default class AuditLogComponent implements OnInit {
  private readonly settingsService = inject(SettingsService);

  readonly logs = signal<AuditLog[]>([]);
  readonly entityTypes = signal<{ label: string; value: string | null }[]>([{ label: '—', value: null }]);
  readonly loading = signal(true);
  readonly entityFilter = signal<string | null>(null);
  readonly dateFrom = signal<Date | null>(null);
  readonly dateTo = signal<Date | null>(null);
  /**
   * Faqat platforma administratori bajargan amallar. Server filtri emas —
   * yozuvlar bir sahifada keladi, qo'shimcha so'rov ortiqcha.
   */
  readonly platformOnly = signal(false);

  readonly visibleLogs = computed(() =>
    this.platformOnly() ? this.logs().filter((l) => l.isPlatformAction) : this.logs()
  );

  ngOnInit(): void {
    this.settingsService.getAuditEntityTypes().subscribe((res) => {
      if (res.success && res.data) {
        this.entityTypes.set([{ label: '—', value: null }, ...res.data.map((t) => ({ label: t, value: t }))]);
      }
    });
    this.loadLogs();
  }

  loadLogs(): void {
    this.loading.set(true);
    this.settingsService
      .getAuditLogs({
        page: 1,
        pageSize: PAGE_SIZE,
        entityType: this.entityFilter(),
        ...this.dateRange(),
      })
      .subscribe({
        next: (res) => {
          this.logs.set(res.success ? (res.data ?? []) : []);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  /**
   * `from`/`to` — VAQT NUQTASI (serverda UTC `CreatedAt` bilan solishtiriladi).
   * Eskisi `YYYY-MM-DD` yuborardi: Toshkentda kunning birinchi 5 soati hisobotdan
   * tushib qolardi. Endi mahalliy kun chegarasi UTC'ga o'giriladi (`date.util`).
   */
  private dateRange(): { from?: string; to?: string } {
    const from = this.dateFrom();
    const to = this.dateTo();
    return {
      from: from ? localDayRangeToUtc(from, from).from : undefined,
      to: to ? localDayRangeToUtc(to, to).to : undefined,
    };
  }

  setEntity(value: string | null): void {
    this.entityFilter.set(value);
    this.loadLogs();
  }

  setFrom(value: Date | null): void {
    this.dateFrom.set(value);
    this.loadLogs();
  }

  setTo(value: Date | null): void {
    this.dateTo.set(value);
    this.loadLogs();
  }

  parse(dateStr: string): Date | null {
    return parseUtc(dateStr);
  }

  /** Guid uzun — jadvalda boshi yetarli, to'liq qiymat `title` da. */
  shortEntityId(id: string): string {
    // Guid v7 ning boshi — vaqt belgisi; farqlovchi qism — oxiri.
    return id.length > 8 ? id.slice(-8) : id;
  }

  /** HTTP metodini StatusBadge CSS kalitiga aylantiradi. */
  actionClass(action: string): string {
    switch (action) {
      case 'POST':
        return 'Confirmed';
      case 'PUT':
        return 'Pending';
      case 'PATCH':
        return 'Internal';
      case 'DELETE':
        return 'Cancelled';
      default:
        return 'Neutral';
    }
  }
}
