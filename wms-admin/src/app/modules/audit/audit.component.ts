import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { PlatformService } from '../../core/services/platform.service';
import { AuditLogEntry } from '../../core/models/audit.model';
import { Tenant } from '../../core/models/tenant.model';
import { utcDateOnly } from '../../core/utils/date.util';

const DEFAULT_PAGE_SIZE = 50;

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [TranslocoDirective, DatePipe, FormsModule, TableModule, Button, Select, DatePicker, ToggleSwitch],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './audit.component.html',
  styleUrl: './audit.component.scss'
})
export default class AuditComponent implements OnInit {
  private service = inject(PlatformService);
  private transloco = inject(TranslocoService);
  private route = inject(ActivatedRoute);

  rows = signal<AuditLogEntry[]>([]);
  total = signal(0);
  loading = signal(false);

  /**
   * Sahifalash **server tomonda** (`p-table` lazy). Audit jadvali eng tez o'sadigan
   * jadval — uni to'liq yuklash bir necha oydan keyin sahifani o'ldiradi.
   */
  readonly pageSize = DEFAULT_PAGE_SIZE;
  private page = signal(1);

  // Filtrlar
  tenantId = signal<number | null>(null);
  entityType = signal<string | null>(null);
  platformOnly = signal(false);
  from = signal<Date | null>(null);
  to = signal<Date | null>(null);

  tenants = signal<Tenant[]>([]);
  entityTypes = signal<string[]>([]);

  tenantOptions = computed(() => this.tenants().map(t => ({ label: t.name, value: t.id })));
  entityTypeOptions = computed(() => this.entityTypes().map(e => ({ label: e, value: e })));

  ngOnInit() {
    // Tenant ro'yxati uchun alohida endpoint yozilmadi — mavjud `admin/tenants`
    // id va nomni allaqachon qaytaradi (B4 hisobotida shunday kelishilgan).
    this.service.getTenants().subscribe(r => { if (r.success && r.data) this.tenants.set(r.data); });
    this.service.getAuditEntityTypes().subscribe(r => { if (r.success && r.data) this.entityTypes.set(r.data); });

    // Tenant detalidan "shu mijozning audit jurnali" havolasi filtr bilan keladi.
    const preset = Number(this.route.snapshot.queryParamMap.get('tenantId'));
    if (preset) this.tenantId.set(preset);
    const platformOnly = this.route.snapshot.queryParamMap.get('platformOnly');
    if (platformOnly === '1') this.platformOnly.set(true);
  }

  /** `p-table` sahifa yoki tartib o'zgarganda chaqiradi. */
  onLazyLoad(event: TableLazyLoadEvent) {
    const first = event.first ?? 0;
    const rows = event.rows || this.pageSize;
    this.page.set(Math.floor(first / rows) + 1);
    this.load();
  }

  /** Filtr o'zgarganda birinchi sahifaga qaytamiz — aks holda bo'sh sahifada qolish mumkin. */
  applyFilters() {
    this.page.set(1);
    this.load();
  }

  clearFilters() {
    this.tenantId.set(null);
    this.entityType.set(null);
    this.platformOnly.set(false);
    this.from.set(null);
    this.to.set(null);
    this.applyFilters();
  }

  private load() {
    this.loading.set(true);
    this.service.getAudit({
      page: this.page(),
      pageSize: this.pageSize,
      tenantId: this.tenantId(),
      entityType: this.entityType(),
      platformOnly: this.platformOnly() || null,
      from: this.from() ? utcDateOnly(this.from()!) : null,
      // Kun oxirigacha: `to` sanasi kiritilsa o'sha kunning yozuvlari ham kirsin.
      to: this.to() ? endOfDay(this.to()!) : null
    }).subscribe({
      next: (r) => {
        this.rows.set(r.success && r.data ? r.data.items : []);
        this.total.set(r.success && r.data ? r.data.totalCount : 0);
        this.loading.set(false);
      },
      // Xato toastini interceptor chiqaradi.
      error: () => this.loading.set(false)
    });
  }

  /** `POST` yashil, `DELETE` qizil — jadvalni ko'z bilan skanerlashni osonlashtiradi. */
  actionClass(action: string): string {
    switch (action?.toUpperCase()) {
      case 'POST': return 'pill pill-success';
      case 'DELETE': return 'pill pill-danger';
      case 'PUT':
      case 'PATCH': return 'pill pill-warning';
      default: return 'pill pill-neutral';
    }
  }

  objectLabel(row: AuditLogEntry): string {
    const parts = [row.entityType, row.entityAction].filter(Boolean);
    return parts.join(' · ');
  }
}

/** Sana filtri kun boshiga tushmasin — `to` kiritilgan kun to'liq qamrab olinadi. */
function endOfDay(d: Date): string {
  const end = new Date(d);
  end.setHours(23, 59, 59, 999);
  return end.toISOString();
}
