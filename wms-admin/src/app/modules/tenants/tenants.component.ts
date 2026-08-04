import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { Textarea } from 'primeng/textarea';
import { Password } from 'primeng/password';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { PlatformService } from '../../core/services/platform.service';
import { NotificationService } from '../../core/services/notification.service';
import {
  Tenant, TenantModuleInfo, SubscriptionStatus,
  PaymentRecord, PaymentMethod, PAYMENT_METHODS
} from '../../core/models/tenant.model';
import { Plan } from '../../core/models/plan.model';
import { utcDateOnly, daysUntil } from '../../core/utils/date.util';

interface TenantForm {
  id?: number; name: string; slug: string;
  adminFullName: string; adminPhone: string; adminPassword: string;
  isActive: boolean; planId: number | null; subscriptionStatus: SubscriptionStatus; trialEndsAt: Date | null;
}

interface PaymentForm {
  periodStart: Date | null; periodEnd: Date | null;
  amount: number; currency: string; method: PaymentMethod; note: string;
}

type TenantFilter = 'all' | 'expiring' | 'expired' | 'nolimit';

@Component({
  selector: 'app-tenants',
  standalone: true,
  imports: [DatePipe, DecimalPipe, FormsModule, TableModule, Button, Dialog, InputText,
    InputNumber, Textarea, Password, Select, DatePicker, ToggleSwitch],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './tenants.component.html',
  styleUrl: './tenants.component.scss'
})
export default class TenantsComponent implements OnInit {
  private service = inject(PlatformService);
  private notify = inject(NotificationService);
  private route = inject(ActivatedRoute);

  tenants = signal<Tenant[]>([]);
  plans = signal<Plan[]>([]);
  loading = signal(true);
  saving = signal(false);

  filter = signal<TenantFilter>('all');
  filterOptions = [
    { label: 'All tenants', value: 'all' as TenantFilter },
    { label: 'Expiring soon (7d)', value: 'expiring' as TenantFilter },
    { label: 'Expired', value: 'expired' as TenantFilter },
    { label: 'No limit', value: 'nolimit' as TenantFilter }
  ];

  visibleTenants = computed(() => {
    const f = this.filter();
    if (f === 'all') return this.tenants();
    return this.tenants().filter(t => {
      const days = this.paidDaysLeft(t);
      if (f === 'nolimit') return days === null;
      if (days === null) return false;
      if (f === 'expired') return days < 0;
      return days >= 0 && days <= 7;
    });
  });

  dialogVisible = signal(false);
  editing = signal(false);
  form = signal<TenantForm>(this.empty());

  modulesVisible = signal(false);
  modulesTenant = signal<Tenant | null>(null);
  modules = signal<TenantModuleInfo[]>([]);

  // Payments
  paymentVisible = signal(false);
  paymentTenant = signal<Tenant | null>(null);
  paymentForm = signal<PaymentForm>(this.emptyPayment());
  historyVisible = signal(false);
  payments = signal<PaymentRecord[]>([]);
  loadingPayments = signal(false);
  readonly methodOptions = PAYMENT_METHODS;

  statusOptions = [
    { label: 'Trial', value: SubscriptionStatus.Trial },
    { label: 'Active', value: SubscriptionStatus.Active },
    { label: 'Suspended', value: SubscriptionStatus.Suspended }
  ];

  private empty(): TenantForm {
    return { name: '', slug: '', adminFullName: '', adminPhone: '', adminPassword: '',
      isActive: true, planId: null, subscriptionStatus: SubscriptionStatus.Trial, trialEndsAt: null };
  }

  private emptyPayment(): PaymentForm {
    return { periodStart: null, periodEnd: null, amount: 0, currency: 'UZS', method: 'BankTransfer', note: '' };
  }

  ngOnInit() {
    this.load();
    this.service.getPlans().subscribe(r => { if (r.success && r.data) this.plans.set(r.data); });
    // Dashboard kartalari shu filtr bilan havola qiladi
    const f = this.route.snapshot.queryParamMap.get('filter') as TenantFilter | null;
    if (f && this.filterOptions.some(o => o.value === f)) this.filter.set(f);
  }

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
        trialEndsAt: f.trialEndsAt ? utcDateOnly(f.trialEndsAt) : null
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

  // ---- Payments -----------------------------------------------------------

  openPayment(t: Tenant) {
    const start = t.paidUntil ? new Date(t.paidUntil) : new Date();
    this.paymentTenant.set(t);
    this.paymentForm.set({ ...this.emptyPayment(), periodStart: start });
    this.paymentVisible.set(true);
  }

  updatePayment(field: keyof PaymentForm, value: unknown) {
    this.paymentForm.update(f => ({ ...f, [field]: value }));
  }

  /** Tez tugmalar — davr oxirini boshidan hisoblaydi. */
  addPeriod(months: number) {
    this.paymentForm.update(f => {
      const start = f.periodStart ?? new Date();
      const end = new Date(start);
      end.setMonth(end.getMonth() + months);
      return { ...f, periodStart: start, periodEnd: end };
    });
  }

  savePayment() {
    const t = this.paymentTenant(); const f = this.paymentForm();
    if (!t) return;
    if (!f.periodStart || !f.periodEnd) { this.notify.warn('Period start and end are required'); return; }
    if (f.periodEnd <= f.periodStart) { this.notify.warn('Period end must be after the start'); return; }
    if (f.amount <= 0) { this.notify.warn('Amount must be greater than zero'); return; }

    const wasSuspendedForNonPayment =
      t.subscriptionStatus === SubscriptionStatus.Suspended && t.suspendReason === 'NonPayment';

    this.saving.set(true);
    this.service.createPayment(t.id, {
      periodStart: utcDateOnly(f.periodStart), periodEnd: utcDateOnly(f.periodEnd),
      amount: f.amount, currency: f.currency, method: f.method, note: f.note.trim() || null
    }).subscribe({
      next: () => {
        this.saving.set(false); this.paymentVisible.set(false);
        this.notify.success(`Paid until ${f.periodEnd!.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' })}`);
        if (wasSuspendedForNonPayment) this.notify.info('Tenant reactivated');
        this.load();
      },
      error: () => this.saving.set(false)
    });
  }

  openHistory(t: Tenant) {
    this.paymentTenant.set(t); this.payments.set([]); this.historyVisible.set(true);
    this.loadingPayments.set(true);
    this.service.getPayments(t.id).subscribe({
      next: (r) => { this.payments.set(r.success && r.data ? r.data : []); this.loadingPayments.set(false); },
      error: () => this.loadingPayments.set(false)
    });
  }

  cancelPayment(p: PaymentRecord) {
    this.notify.confirmDelete(
      'Cancel this payment record? Paid until will be recalculated from the remaining records.',
      () => {
        this.service.deletePayment(p.id).subscribe(() => {
          this.notify.success('Payment record cancelled');
          this.payments.update(list => list.filter(x => x.id !== p.id));
          this.load();
        });
      }
    );
  }

  // ---- Trial / paid columns ----------------------------------------------

  trialDaysLeft(t: Tenant): number | null { return daysUntil(t.trialEndsAt); }

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
    return days < 0 ? `${-days}d overdue` : `${days}d left`;
  }

  paidDaysLeft(t: Tenant): number | null { return daysUntil(t.paidUntil); }

  paidClass(t: Tenant): string {
    const days = this.paidDaysLeft(t);
    if (days === null) return '';
    if (days < 3) return 'pill pill-danger';
    if (days < 7) return 'pill pill-warning';
    return 'pill pill-neutral';
  }

  paidLabel(t: Tenant): string {
    const days = this.paidDaysLeft(t);
    if (days === null) return '';
    if (days < 0) return 'expired';
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
