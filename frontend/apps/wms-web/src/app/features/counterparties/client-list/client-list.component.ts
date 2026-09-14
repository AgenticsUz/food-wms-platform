import { ChangeDetectionStrategy, Component, type OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { Textarea } from 'primeng/textarea';
import { Select } from 'primeng/select';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';

import { WmsSession } from '../../../core/auth/wms-session';
import { NotificationService } from '../../../core/notify/notification.service';
import { ExportService } from '../../../core/services/export.service';
import { toLocalDateString } from '../../../core/utils/date.util';
import { SEARCH_CALL, onSearchChange } from '../../../core/utils/search.util';
import { ImportButtonComponent } from '../../../shared/components/import-button/import-button.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { PhoneInputComponent } from '../../../shared/components/phone-input/phone-input.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import type { Agent } from '../../agents/agent.model';
import { AgentService } from '../../agents/agent.service';
import {
  counterpartyFormOf,
  counterpartySaveDtoOf,
  emptyCounterpartyForm,
  isInnInvalid,
  type CounterpartyForm,
} from '../counterparty-form';
import { CounterpartyType, type Counterparty } from '../counterparty.model';
import { CounterpartyService } from '../counterparty.service';

@Component({
  selector: 'app-client-list',
  imports: [
    FormsModule, TableModule, Button, InputText, Dialog, Textarea, Select, TranslocoDirective,
    PageHeaderComponent, HasPermissionDirective, ImportButtonComponent, PhoneInputComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './client-list.component.html',
  styleUrl: './client-list.component.scss',
})
export default class ClientListComponent implements OnInit {
  private readonly service = inject(CounterpartyService);
  private readonly agentService = inject(AgentService);
  private readonly session = inject(WmsSession);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);
  private readonly router = inject(Router);
  readonly exportService = inject(ExportService);

  readonly items = signal<Counterparty[]>([]);
  readonly agents = signal<Agent[]>([]);
  readonly loading = signal(true);
  readonly search = signal('');
  readonly dialogVisible = signal(false);
  readonly editing = signal(false);
  readonly saving = signal(false);

  readonly form = signal<CounterpartyForm>(emptyCounterpartyForm(CounterpartyType.Client));

  readonly innInvalid = computed(() => isInnInvalid(this.form().inn));

  constructor() {
    // Qidiruv endi SERVERDA (nom/telefon/INN) — mijozdagi `includes` alifboni
    // bilmasdi. 300 ms kutish: har harfga so'rov ketmasin (search.util.ts).
    onSearchChange(this.search, () => this.loadData());
  }

  ngOnInit(): void {
    this.loadData();
    this.loadAgents();
  }

  /**
   * Agentlar ro'yxati — `AGENTS` moduli va `agents.view` ruxsati ortida, mijozlar
   * esa ularsiz ham ochiladi. Modul yo'q bo'lsa so'rov yuborilmaydi (403 toasti
   * mijozlar sahifasida chiqmasin), ruxsat yo'q bo'lsa jim o'tadi.
   */
  private loadAgents(): void {
    if (!this.session.isModuleEnabled('AGENTS')) return;
    this.agentService.getAgents({ skipErrorNotify: true }).subscribe({
      next: (res) => this.agents.set((res.data ?? []).filter((a) => a.isActive)),
      error: () => undefined,
    });
  }

  loadData(): void {
    const query = this.search().trim();
    this.loading.set(true);
    // Qidiruvda global progress chizig'i chaqnamasin (`SEARCH_CALL`) — jadvalning
    // o'z `loading` i yetarli.
    this.service
      .getCounterparties(CounterpartyType.Client, query, query ? SEARCH_CALL : undefined)
      .subscribe({
        next: (res) => {
          this.items.set(res.success && res.data ? res.data : []);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  openNew(): void {
    this.form.set(emptyCounterpartyForm(CounterpartyType.Client));
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(c: Counterparty): void {
    this.form.set(counterpartyFormOf(c));
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save(): void {
    if (this.innInvalid()) {
      this.notify.warn(this.language.translate('partners.innInvalid'));
      return;
    }
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn('Name is required');
      return;
    }
    this.saving.set(true);
    const dto = counterpartySaveDtoOf(f);
    const request =
      this.editing() && f.id !== null ? this.service.updateCounterparty(f.id, dto) : this.service.createCounterparty(dto);
    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.editing() ? 'Client updated' : 'Client created');
        this.loadData();
      },
      error: () => this.saving.set(false),
    });
  }

  deleteItem(c: Counterparty): void {
    this.notify.confirmDelete(`Delete "${c.name}"?`, () => {
      this.service.deleteCounterparty(c.id).subscribe({
        next: () => {
          this.notify.success('Client deleted');
          this.loadData();
        },
        // Xato toastini qobiq chiqaradi.
        error: () => undefined,
      });
    });
  }

  viewDetail(c: Counterparty): void {
    void this.router.navigate(['/counterparties', c.id]);
  }

  exportClients(): void {
    this.exportService.download('export/counterparties', `clients-${toLocalDateString(new Date())}.xlsx`, {
      type: CounterpartyType.Client,
    });
  }

  updateForm<K extends keyof CounterpartyForm>(field: K, value: CounterpartyForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }
}
