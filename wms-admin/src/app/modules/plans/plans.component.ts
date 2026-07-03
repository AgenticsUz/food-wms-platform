import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { MultiSelect } from 'primeng/multiselect';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { PlatformService } from '../../core/services/platform.service';
import { NotificationService } from '../../core/services/notification.service';
import { Plan } from '../../core/models/plan.model';
import { ModuleInfo } from '../../core/models/plan.model';

interface PlanForm {
  id?: number; name: string; code: string; price: number; isActive: boolean;
  moduleCodes: string[]; maxUsers: number; maxWarehouses: number; maxTransfersPerMonth: number;
}

@Component({
  selector: 'app-plans',
  standalone: true,
  imports: [DecimalPipe, FormsModule, TableModule, Button, Dialog, InputText, InputNumber, MultiSelect, ToggleSwitch],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './plans.component.html',
  styleUrl: '../tenants/tenants.component.scss'
})
export default class PlansComponent implements OnInit {
  private service = inject(PlatformService);
  private notify = inject(NotificationService);

  plans = signal<Plan[]>([]);
  modules = signal<ModuleInfo[]>([]);
  loading = signal(true);
  saving = signal(false);
  dialogVisible = signal(false);
  editing = signal(false);
  form = signal<PlanForm>(this.empty());

  private empty(): PlanForm {
    return { name: '', code: '', price: 0, isActive: true, moduleCodes: [], maxUsers: 10, maxWarehouses: 3, maxTransfersPerMonth: 1000 };
  }

  ngOnInit() {
    this.load();
    this.service.getModules().subscribe(r => { if (r.success && r.data) this.modules.set(r.data); });
  }

  moduleOptions() { return this.modules().map(m => ({ label: m.moduleName, value: m.moduleCode })); }

  load() {
    this.loading.set(true);
    this.service.getPlans().subscribe({
      next: (r) => { this.plans.set(r.success && r.data ? r.data : []); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openNew() { this.form.set(this.empty()); this.editing.set(false); this.dialogVisible.set(true); }
  openEdit(p: Plan) {
    this.form.set({ id: p.id, name: p.name, code: p.code, price: p.price, isActive: p.isActive,
      moduleCodes: [...p.moduleCodes], maxUsers: p.maxUsers, maxWarehouses: p.maxWarehouses, maxTransfersPerMonth: p.maxTransfersPerMonth });
    this.editing.set(true); this.dialogVisible.set(true);
  }
  updateForm(field: keyof PlanForm, value: unknown) { this.form.update(f => ({ ...f, [field]: value })); }

  save() {
    const f = this.form();
    if (!f.name.trim() || !f.code.trim()) { this.notify.warn('Name and code are required'); return; }
    this.saving.set(true);
    const dto = { name: f.name.trim(), code: f.code.trim(), price: f.price, isActive: f.isActive,
      moduleCodes: f.moduleCodes, maxUsers: f.maxUsers, maxWarehouses: f.maxWarehouses, maxTransfersPerMonth: f.maxTransfersPerMonth };
    const obs = this.editing() ? this.service.updatePlan(f.id!, dto) : this.service.createPlan(dto);
    obs.subscribe({
      next: () => { this.saving.set(false); this.dialogVisible.set(false); this.notify.success('Saved'); this.load(); },
      error: () => this.saving.set(false)
    });
  }

  remove(p: Plan) {
    this.notify.confirmDelete(`Delete plan "${p.name}"?`, () => {
      this.service.deletePlan(p.id).subscribe(() => { this.notify.success('Deleted'); this.load(); });
    });
  }
}
