import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { PlatformService } from '../../core/services/platform.service';
import { NotificationService } from '../../core/services/notification.service';
import { Tenant, TenantModuleInfo, SubscriptionStatus } from '../../core/models/tenant.model';
import { Plan } from '../../core/models/plan.model';

interface TenantForm {
  id?: number; name: string; slug: string;
  adminFullName: string; adminPhone: string; adminPassword: string;
  isActive: boolean; planId: number | null; subscriptionStatus: SubscriptionStatus; trialEndsAt: Date | null;
}

@Component({
  selector: 'app-tenants',
  standalone: true,
  imports: [DatePipe, FormsModule, TableModule, Button, Dialog, InputText, Password, Select, DatePicker, ToggleSwitch],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './tenants.component.html',
  styleUrl: './tenants.component.scss'
})
export default class TenantsComponent implements OnInit {
  private service = inject(PlatformService);
  private notify = inject(NotificationService);

  tenants = signal<Tenant[]>([]);
  plans = signal<Plan[]>([]);
  loading = signal(true);
  saving = signal(false);

  dialogVisible = signal(false);
  editing = signal(false);
  form = signal<TenantForm>(this.empty());

  modulesVisible = signal(false);
  modulesTenant = signal<Tenant | null>(null);
  modules = signal<TenantModuleInfo[]>([]);

  statusOptions = [
    { label: 'Trial', value: SubscriptionStatus.Trial },
    { label: 'Active', value: SubscriptionStatus.Active },
    { label: 'Suspended', value: SubscriptionStatus.Suspended }
  ];

  private empty(): TenantForm {
    return { name: '', slug: '', adminFullName: '', adminPhone: '', adminPassword: '',
      isActive: true, planId: null, subscriptionStatus: SubscriptionStatus.Trial, trialEndsAt: null };
  }

  ngOnInit() { this.load(); this.service.getPlans().subscribe(r => { if (r.success && r.data) this.plans.set(r.data); }); }

  load() {
    this.loading.set(true);
    this.service.getTenants().subscribe({
      next: (r) => { this.tenants.set(r.success && r.data ? r.data : []); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  planOptions() { return this.plans().map(p => ({ label: p.name, value: p.id })); }
  planName(id: number | null) { return this.plans().find(p => p.id === id)?.name ?? '—'; }

  openNew() { this.form.set(this.empty()); this.editing.set(false); this.dialogVisible.set(true); }
  openEdit(t: Tenant) {
    this.form.set({
      id: t.id, name: t.name, slug: t.slug, adminFullName: '', adminPhone: '', adminPassword: '',
      isActive: t.isActive, planId: t.planId, subscriptionStatus: t.subscriptionStatus,
      trialEndsAt: t.trialEndsAt ? new Date(t.trialEndsAt) : null
    });
    this.editing.set(true); this.dialogVisible.set(true);
  }
  updateForm(field: keyof TenantForm, value: unknown) { this.form.update(f => ({ ...f, [field]: value })); }

  save() {
    const f = this.form();
    if (!f.name.trim() || !f.slug.trim()) { this.notify.warn('Name and slug are required'); return; }
    this.saving.set(true);
    if (this.editing()) {
      this.service.updateTenant(f.id!, {
        name: f.name.trim(), slug: f.slug.trim(), isActive: f.isActive, planId: f.planId,
        subscriptionStatus: f.subscriptionStatus,
        trialEndsAt: f.trialEndsAt ? this.dateStr(f.trialEndsAt) : null
      }).subscribe({ next: () => this.afterSave(), error: () => this.saving.set(false) });
    } else {
      if (!f.adminFullName.trim() || !f.adminPhone.trim() || f.adminPassword.length < 6) {
        this.saving.set(false); this.notify.warn('Admin name, phone and a 6+ char password are required'); return;
      }
      this.service.createTenant({
        name: f.name.trim(), slug: f.slug.trim(), adminFullName: f.adminFullName.trim(),
        adminPhone: f.adminPhone.trim(), adminPassword: f.adminPassword, planId: f.planId
      }).subscribe({ next: () => this.afterSave(), error: () => this.saving.set(false) });
    }
  }
  private afterSave() { this.saving.set(false); this.dialogVisible.set(false); this.notify.success('Saved'); this.load(); }
  private dateStr(d: Date) { return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; }

  suspend(t: Tenant) { this.service.suspendTenant(t.id).subscribe(() => { this.notify.success('Suspended'); this.load(); }); }
  activate(t: Tenant) { this.service.activateTenant(t.id).subscribe(() => { this.notify.success('Activated'); this.load(); }); }

  remove(t: Tenant) {
    this.notify.confirmDelete(`Delete "${t.name}"?`, () => {
      this.service.deleteTenant(t.id).subscribe(() => { this.notify.success('Deleted'); this.load(); });
    });
  }

  openModules(t: Tenant) {
    this.modulesTenant.set(t); this.modules.set([]); this.modulesVisible.set(true);
    this.service.getTenantModules(t.id).subscribe(r => { if (r.success && r.data) this.modules.set(r.data); });
  }
  toggleModule(m: TenantModuleInfo, enabled: boolean) {
    const t = this.modulesTenant(); if (!t) return;
    this.service.toggleModule(t.id, m.moduleId, enabled).subscribe(() => {
      this.modules.update(list => list.map(x => x.moduleId === m.moduleId ? { ...x, isEnabled: enabled } : x));
    });
  }

  /** Trial tugashiga qolgan kun. null — trial sanasi yo'q. */
  trialDaysLeft(t: Tenant): number | null {
    if (!t.trialEndsAt) return null;
    const ends = new Date(t.trialEndsAt).getTime();
    return Math.ceil((ends - Date.now()) / 86_400_000);
  }

  trialClass(t: Tenant): string {
    const days = this.trialDaysLeft(t);
    if (days === null) return '';
    if (days < 3) return 'pill pill-danger';
    if (days < 7) return 'pill pill-warning';
    return 'pill pill-neutral';
  }

  trialLabel(t: Tenant): string {
    const days = this.trialDaysLeft(t);
    if (days === null) return '';
    if (days < 0) return `${-days}d overdue`;
    return `${days}d left`;
  }

  /** Modul yoqilgan, lekin tenant planining to'plamiga kirmaydi — qo'lda yoqilgan. */
  isOutsidePlan(m: TenantModuleInfo): boolean {
    if (!m.isEnabled) return false;
    const planId = this.modulesTenant()?.planId;
    if (!planId) return false;
    const plan = this.plans().find(p => p.id === planId);
    if (!plan) return false;
    return !plan.moduleCodes.includes(m.moduleCode);
  }

  readonly S = SubscriptionStatus;
  statusLabel(s: SubscriptionStatus) { return SubscriptionStatus[s] ?? '—'; }
  statusClass(s: SubscriptionStatus) {
    return s === SubscriptionStatus.Active ? 'pill pill-success' : s === SubscriptionStatus.Trial ? 'pill pill-warning' : 'pill pill-danger';
  }
}
