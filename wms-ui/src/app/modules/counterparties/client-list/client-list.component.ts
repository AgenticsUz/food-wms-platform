import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { toLocalDateString } from '../../../shared/utils/date.util';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Textarea } from 'primeng/textarea';
import { Select } from 'primeng/select';
import { TranslocoDirective } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { CounterpartyService } from '../../../core/services/counterparty.service';
import { AgentService } from '../../../core/services/agent.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { ExportService } from '../../../core/services/export.service';
import { ImportButtonComponent } from '../../../shared/components/import-button/import-button.component';
import { PhoneInputComponent } from '../../../shared/components/phone-input/phone-input.component';
import { Counterparty, CounterpartyCreateDto, CounterpartyType } from '../../../core/models/counterparty.model';
import { Agent } from '../../../core/models/agent.model';

@Component({
  selector: 'app-client-list',
  standalone: true,
  imports: [FormsModule, TableModule, Button, InputText, Dialog, ToggleSwitch, Textarea, Select, TranslocoDirective, PageHeaderComponent, HasPermissionDirective, ImportButtonComponent, PhoneInputComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './client-list.component.html',
  styleUrl: './client-list.component.scss'
})
export default class ClientListComponent implements OnInit {
  private service = inject(CounterpartyService);
  private agentService = inject(AgentService);
  private notify = inject(NotificationService);
  private router = inject(Router);
  exportService = inject(ExportService);

  items = signal<Counterparty[]>([]);
  filtered = signal<Counterparty[]>([]);
  agents = signal<Agent[]>([]);
  loading = signal(true);
  search = signal('');
  dialogVisible = signal(false);
  editing = signal(false);
  saving = signal(false);

  form = signal<CounterpartyCreateDto & { id?: number }>({
    name: '', type: CounterpartyType.Client, phone: null, address: null,
    note: null, portalPhone: null, portalEnabled: false, agentId: null
  });

  ngOnInit() { this.loadData(); this.loadAgents(); }

  private loadAgents() {
    this.agentService.getAgents().subscribe({
      next: (res) => { if (res.success && res.data) this.agents.set(res.data.filter(a => a.isActive)); }
    });
  }

  loadData() {
    this.loading.set(true);
    this.service.getCounterparties({ type: CounterpartyType.Client }).subscribe({
      next: (res) => {
        const list = res.success && res.data ? res.data : [];
        this.items.set(list);
        this.applyFilter();
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.notify.error('Failed to load clients'); }
    });
  }

  applyFilter() {
    const q = this.search().toLowerCase();
    this.filtered.set(q ? this.items().filter(c =>
      c.name.toLowerCase().includes(q) || (c.phone && c.phone.includes(q))
    ) : this.items());
  }

  onSearch(value: string) { this.search.set(value); this.applyFilter(); }

  openNew() {
    this.form.set({ name: '', type: CounterpartyType.Client, phone: null, address: null, note: null, portalPhone: null, portalEnabled: false, agentId: null });
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(c: Counterparty) {
    this.form.set({ id: c.id, name: c.name, type: c.type, phone: c.phone, address: c.address, note: c.note, portalPhone: c.portalPhone, portalEnabled: c.portalEnabled, agentId: c.agentId });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.name.trim()) { this.notify.warn('Name is required'); return; }
    this.saving.set(true);
    const dto: CounterpartyCreateDto = { name: f.name, type: f.type, phone: f.phone, address: f.address, note: f.note, portalPhone: f.portalPhone, portalEnabled: f.portalEnabled, agentId: f.agentId ?? null };
    const obs = this.editing() ? this.service.updateCounterparty(f.id!, dto) : this.service.createCounterparty(dto);
    obs.subscribe({
      next: () => { this.saving.set(false); this.dialogVisible.set(false); this.notify.success(this.editing() ? 'Client updated' : 'Client created'); this.loadData(); },
      error: () => { this.saving.set(false); this.notify.error('Failed to save client'); }
    });
  }

  deleteItem(c: Counterparty) {
    this.notify.confirmDelete(`Delete "${c.name}"?`, () => {
      this.service.deleteCounterparty(c.id).subscribe({
        next: () => { this.notify.success('Client deleted'); this.loadData(); },
        error: () => this.notify.error('Failed to delete client')
      });
    });
  }

  viewDetail(c: Counterparty) { this.router.navigate(['/counterparties', c.id]); }

  exportClients() {
    this.exportService.download('export/counterparties', `clients-${toLocalDateString(new Date())}.xlsx`, { type: 'Client' });
  }

  updateForm(field: string, value: unknown) { this.form.update(f => ({ ...f, [field]: value })); }
}
