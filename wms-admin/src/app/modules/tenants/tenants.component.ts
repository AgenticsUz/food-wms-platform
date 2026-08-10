import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
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
import { RadioButton } from 'primeng/radiobutton';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { PlatformService } from '../../core/services/platform.service';
import { NotificationService } from '../../core/services/notification.service';
import {
  Tenant, TenantModuleInfo, SubscriptionStatus,
  PaymentRecord, PaymentMethod, PAYMENT_METHODS,
  SuspendReason, SUSPEND_REASONS,
  TenantUser, PasswordResetResult
} from '../../core/models/tenant.model';
import { Plan } from '../../core/models/plan.model';
import { TenantFeature } from '../../core/models/feature.model';
import { utcDateOnly, daysUntil } from '../../core/utils/date.util';

interface TenantForm {
  id?: number; name: string; slug: string; inn: string;
  adminFullName: string; adminPhone: string; adminPassword: string;
  isActive: boolean; planId: number | null; subscriptionStatus: SubscriptionStatus; trialEndsAt: Date | null;
}

interface PaymentForm {
  periodStart: Date | null; periodEnd: Date | null;
  amount: number; currency: string; method: PaymentMethod; note: string;
}

interface SuspendForm {
  reason: SuspendReason | null;
  note: string;
  publicMessage: string;
  temporary: boolean;
  until: Date | null;
}

/** Parolni avtomatik yaratish yoki qo'lda kiritish. */
type ResetMode = 'auto' | 'manual';

interface ResetForm {
  userId: number | null;
  mode: ResetMode;
  password: string;
}

/** Backenddagi `PasswordGenerator.MinimumManualLength` bilan bir xil. */
const MIN_PASSWORD_LENGTH = 8;

/** Tenant admini shu nomdagi rol bilan yaratiladi (`TenantProvisioner`). */
const ADMIN_ROLE = 'admin';

type TenantFilter = 'all' | 'expiring' | 'expired' | 'nolimit' | 'suspended';

/** Modulga bog'lanmagan feature'lar guruhi (backend `moduleCode: null` yuboradi). */
const GENERAL_GROUP = 'GENERAL';

@Component({
  selector: 'app-tenants',
  standalone: true,
  imports: [TranslocoDirective, DatePipe, DecimalPipe, FormsModule, TableModule, Button, Dialog, InputText,
    InputNumber, Textarea, Password, Select, DatePicker, ToggleSwitch, RadioButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './tenants.component.html',
  styleUrl: './tenants.component.scss'
})
export default class TenantsComponent implements OnInit {
  private service = inject(PlatformService);
  private notify = inject(NotificationService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private transloco = inject(TranslocoService);

  tenants = signal<Tenant[]>([]);
  plans = signal<Plan[]>([]);
  loading = signal(true);
  saving = signal(false);

  filter = signal<TenantFilter>('all');
  reasonFilter = signal<SuspendReason | null>(null);
  private readonly filterKeys: { key: string; value: TenantFilter }[] = [
    { key: 'tenants.filterAll', value: 'all' },
    { key: 'tenants.filterExpiring', value: 'expiring' },
    { key: 'tenants.filterExpired', value: 'expired' },
    { key: 'tenants.filterNoLimit', value: 'nolimit' },
    { key: 'tenants.filterSuspended', value: 'suspended' }
  ];
  filterOptions = computed(() =>
    this.filterKeys.map(o => ({ label: this.transloco.translate(o.key), value: o.value })));
  reasonOptions = computed(() =>
    SUSPEND_REASONS.map(o => ({ label: this.transloco.translate('suspendReason.' + o.value), value: o.value })));

  visibleTenants = computed(() => {
    const f = this.filter();
    if (f === 'all') return this.tenants();
    if (f === 'suspended') {
      const reason = this.reasonFilter();
      return this.tenants().filter(t =>
        t.subscriptionStatus === SubscriptionStatus.Suspended &&
        (!reason || t.suspendReason === reason));
    }
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
  methodOptions = computed(() =>
    PAYMENT_METHODS.map(o => ({ label: this.transloco.translate('paymentMethod.' + o.value), value: o.value })));

  statusOptions = computed(() => [
    { label: this.transloco.translate('status.Trial'), value: SubscriptionStatus.Trial },
    { label: this.transloco.translate('status.Active'), value: SubscriptionStatus.Active },
    { label: this.transloco.translate('status.Suspended'), value: SubscriptionStatus.Suspended }
  ]);

  private empty(): TenantForm {
    return { name: '', slug: '', inn: '', adminFullName: '', adminPhone: '', adminPassword: '',
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
    if (f && this.filterKeys.some(o => o.value === f)) this.filter.set(f);
  }

  load() {
    this.loading.set(true);
    this.service.getTenants().subscribe({
      next: (r) => { this.tenants.set(r.success && r.data ? r.data : []); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  /** O'zbekiston STIR — aynan 9 raqam yoki bo'sh. */
  innInvalid = computed(() => {
    const inn = this.form().inn.trim();
    return !!inn && !/^[0-9]{9}$/.test(inn);
  });

  planOptions() { return this.plans().map(p => ({ label: p.name, value: p.id })); }
  planName(id: number | null) { return this.plans().find(p => p.id === id)?.name ?? '—'; }

  openNew() { this.form.set(this.empty()); this.editing.set(false); this.dialogVisible.set(true); }
  openEdit(t: Tenant) {
    this.form.set({
      id: t.id, name: t.name, slug: t.slug, inn: t.inn ?? '', adminFullName: '', adminPhone: '', adminPassword: '',
      isActive: t.isActive, planId: t.planId, subscriptionStatus: t.subscriptionStatus,
      trialEndsAt: t.trialEndsAt ? new Date(t.trialEndsAt) : null
    });
    this.editing.set(true); this.dialogVisible.set(true);
  }
  updateForm(field: keyof TenantForm, value: unknown) { this.form.update(f => ({ ...f, [field]: value })); }

  save() {
    const f = this.form();
    if (!f.name.trim() || !f.slug.trim()) { this.notify.warn(this.transloco.translate('tenants.nameRequired')); return; }
    if (this.innInvalid()) { this.notify.warn(this.transloco.translate('tenants.tinInvalid')); return; }
    this.saving.set(true);
    if (this.editing()) {
      this.service.updateTenant(f.id!, {
        name: f.name.trim(), slug: f.slug.trim(), inn: f.inn.trim() || null, isActive: f.isActive, planId: f.planId,
        subscriptionStatus: f.subscriptionStatus,
        trialEndsAt: f.trialEndsAt ? utcDateOnly(f.trialEndsAt) : null
      }).subscribe({ next: () => this.afterSave(), error: () => this.saving.set(false) });
    } else {
      if (!f.adminFullName.trim() || !f.adminPhone.trim() || f.adminPassword.length < 6) {
        this.saving.set(false); this.notify.warn(this.transloco.translate('tenants.adminRequired')); return;
      }
      this.service.createTenant({
        name: f.name.trim(), slug: f.slug.trim(), inn: f.inn.trim() || null,
        adminFullName: f.adminFullName.trim(),
        adminPhone: f.adminPhone.trim(), adminPassword: f.adminPassword, planId: f.planId
      }).subscribe({ next: () => this.afterSave(), error: () => this.saving.set(false) });
    }
  }
  private afterSave() { this.saving.set(false); this.dialogVisible.set(false); this.notify.success(this.transloco.translate('tenants.saved')); this.load(); }

  // ---- Suspend ------------------------------------------------------------

  suspendVisible = signal(false);
  suspendTenantRef = signal<Tenant | null>(null);
  suspendForm = signal<SuspendForm>(this.emptySuspend());

  private emptySuspend(): SuspendForm {
    return { reason: null, note: '', publicMessage: '', temporary: false, until: null };
  }

  openSuspend(t: Tenant) {
    this.suspendTenantRef.set(t);
    this.suspendForm.set(this.emptySuspend());
    this.suspendVisible.set(true);
  }

  updateSuspend(field: keyof SuspendForm, value: unknown) {
    this.suspendForm.update(f => ({ ...f, [field]: value }));
  }

  /** Sabab tanlanmaguncha yuborib bo'lmaydi. */
  canSuspend = computed(() => {
    const f = this.suspendForm();
    return !!f.reason && (!f.temporary || !!f.until);
  });

  confirmSuspend() {
    const t = this.suspendTenantRef(); const f = this.suspendForm();
    if (!t || !f.reason) return;
    this.saving.set(true);
    this.service.suspendTenant(t.id, {
      reason: f.reason,
      note: f.note.trim() || null,
      publicMessage: f.publicMessage.trim() || null,
      until: f.temporary && f.until ? utcDateOnly(f.until) : null
    }).subscribe({
      next: () => {
        this.saving.set(false); this.suspendVisible.set(false);
        this.notify.success(f.temporary && f.until
          ? this.transloco.translate('tenants.suspend.suspendedUntil', { date: this.formatDate(f.until) })
          : this.transloco.translate('tenants.suspend.suspended'));
        this.load();
      },
      error: () => this.saving.set(false)
    });
  }

  /** Shu mijozning audit jurnali — filtr oldindan qo'yilgan holda. */
  openAudit(t: Tenant) {
    this.router.navigate(['/audit'], { queryParams: { tenantId: t.id } });
  }

  activate(t: Tenant) { this.service.activateTenant(t.id).subscribe(() => { this.notify.success(this.transloco.translate('tenants.activated')); this.load(); }); }

  reasonLabel(reason: SuspendReason | null): string {
    return reason ? this.transloco.translate('suspendReason.' + reason) : '';
  }

  /** Sana toastlarda bir xil ko'rinsin — brauzer lokali emas, tanlangan til. */
  private formatDate(d: Date): string {
    return d.toLocaleDateString(this.transloco.getActiveLang(), {
      day: 'numeric', month: 'short', year: 'numeric'
    });
  }

  remove(t: Tenant) {
    this.notify.confirmDelete(this.transloco.translate('tenants.deleteConfirm', { name: t.name }), () => {
      this.service.deleteTenant(t.id).subscribe(() => {
        this.notify.success(this.transloco.translate('tenants.deleted'));
        this.load();
      });
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

  // ---- Reset user password ------------------------------------------------
  // Mijoz o'z tizimidan qulflanib qolganda telefon orqali qaytarishning yagona yo'li.
  // Yangi parol javobda bir marta keladi — shuning uchun u toast'ga ham, konsolga ham
  // chiqmaydi va natija ekrani operator "Done" bosmaguncha yopilmaydi.

  resetVisible = signal(false);
  resetTenantRef = signal<Tenant | null>(null);
  resetUsers = signal<TenantUser[]>([]);
  loadingUsers = signal(false);
  resetForm = signal<ResetForm>(this.emptyReset());
  resetResult = signal<PasswordResetResult | null>(null);
  resetting = signal(false);
  /** Qaysi maydon hozirgina nusxalandi — tugmadagi belgi shu bo'yicha almashadi. */
  copiedField = signal<'login' | 'password' | null>(null);

  readonly minPasswordLength = MIN_PASSWORD_LENGTH;

  private emptyReset(): ResetForm {
    return { userId: null, mode: 'auto', password: '' };
  }

  openReset(t: Tenant) {
    this.resetTenantRef.set(t);
    this.resetForm.set(this.emptyReset());
    this.resetResult.set(null);
    this.copiedField.set(null);
    this.resetUsers.set([]);
    this.resetVisible.set(true);
    this.loadingUsers.set(true);
    this.service.getTenantUsers(t.id).subscribe({
      next: (r) => {
        const users = r.success && r.data ? r.data : [];
        this.resetUsers.set(users);
        this.resetForm.update(f => ({ ...f, userId: this.defaultUserId(users) }));
        this.loadingUsers.set(false);
      },
      error: () => this.loadingUsers.set(false)
    });
  }

  updateReset(field: keyof ResetForm, value: unknown) {
    this.resetForm.update(f => ({ ...f, [field]: value }));
  }

  /**
   * Backend `userId` berilmasa "eng eski faol admin" ni tanlaydi — dialog ochilganda
   * aynan shu odam tanlangan bo'lsin, operator ro'yxatni qidirmasin. Ro'yxat backenddan
   * `Id` bo'yicha tartiblangan holda keladi, shuning uchun birinchi mos kelgan — eng eskisi.
   */
  private defaultUserId(users: TenantUser[]): number | null {
    const active = users.filter(u => u.isActive);
    const admin = active.find(u => u.roles.some(r => r.trim().toLowerCase() === ADMIN_ROLE));
    return (admin ?? active[0] ?? users[0])?.id ?? null;
  }

  userOptions = computed(() => this.resetUsers().map(u => ({
    label: `${u.fullName} · ${u.login}`,
    value: u.id,
    user: u
  })));

  selectedUser = computed(() => {
    const id = this.resetForm().userId;
    return id === null ? null : this.resetUsers().find(u => u.id === id) ?? null;
  });

  /** Qo'lda kiritilgan parol backend rad etadigan uzunlikda — yuborishdan oldin aytamiz. */
  manualPasswordTooShort = computed(() => {
    const f = this.resetForm();
    return f.mode === 'manual' && f.password.trim().length > 0 && f.password.trim().length < MIN_PASSWORD_LENGTH;
  });

  canReset = computed(() => {
    const f = this.resetForm();
    if (f.userId === null || this.loadingUsers()) return false;
    return f.mode === 'auto' || f.password.trim().length >= MIN_PASSWORD_LENGTH;
  });

  doReset() {
    const t = this.resetTenantRef(); const f = this.resetForm();
    if (!t || !this.canReset()) return;
    this.resetting.set(true);
    this.service.resetUserPassword(t.id, {
      userId: f.userId,
      newPassword: f.mode === 'manual' ? f.password.trim() : null
    }).subscribe({
      // Dialog ataylab ochiq qoladi: parol boshqa hech qayerdan olinmaydi.
      next: (r) => {
        this.resetting.set(false);
        if (r.success && r.data) this.resetResult.set(r.data);
      },
      // Xato toastini interceptor chiqaradi — bu yerda ikkinchisini qo'shmaymiz.
      error: () => this.resetting.set(false)
    });
  }

  /**
   * Forma va natija — ikki alohida `p-dialog`. Buning sababi PrimeNG'ning xatti-harakati:
   * `closable`/`closeOnEscape` tinglovchilari dialog OCHILGANDA bir marta bog'lanadi va
   * keyin bu inputlar o'zgarsa ham qayta ko'rib chiqilmaydi. Bitta dialogda bayroqlarni
   * natija kelgach `false` ga o'tkazish yetarli emas edi — Esc baribir yopib, parolni
   * qaytarib bo'lmaydigan qilib yo'qotardi. Alohida dialog esa boshidanoq yopilmaydigan
   * bo'lib yaratiladi.
   */
  resetFormVisible = computed(() => this.resetVisible() && !this.resetResult());

  /** Natija kelganda forma dialogi o'zi yopiladi — bu holatni tozalash deb hisoblamaymiz. */
  onResetFormVisibleChange(visible: boolean) {
    if (!visible && !this.resetResult()) this.closeReset();
  }

  closeReset() {
    this.resetVisible.set(false);
    this.resetResult.set(null);
    this.copiedField.set(null);
    this.resetForm.set(this.emptyReset());
  }

  async copyValue(value: string, field: 'login' | 'password') {
    if (!await this.writeToClipboard(value)) {
      this.notify.warn(this.transloco.translate('tenants.reset.copyFailed'));
      return;
    }
    this.copiedField.set(field);
    // Toast'da faqat "nusxalandi" — qiymatning o'zi hech qachon toastga tushmaydi.
    this.notify.success(this.transloco.translate('tenants.reset.copied'));
    setTimeout(() => { if (this.copiedField() === field) this.copiedField.set(null); }, 2000);
  }

  /**
   * Clipboard API faqat xavfsiz kontekstda (https yoki localhost) ishlaydi. Konsol oddiy
   * http orqali ochilgan bo'lsa operatorni parolni qo'lda ko'chirishga majburlamaslik uchun
   * eski `execCommand` usuli zaxira sifatida qoladi.
   */
  private async writeToClipboard(value: string): Promise<boolean> {
    try {
      if (navigator.clipboard?.writeText) { await navigator.clipboard.writeText(value); return true; }
    } catch { /* quyidagi zaxira usulga o'tamiz */ }
    try {
      const ta = document.createElement('textarea');
      ta.value = value;
      ta.setAttribute('readonly', '');
      ta.style.position = 'fixed';
      ta.style.opacity = '0';
      document.body.appendChild(ta);
      ta.select();
      const ok = document.execCommand('copy');
      document.body.removeChild(ta);
      return ok;
    } catch { return false; }
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
    if (!f.periodStart || !f.periodEnd) { this.notify.warn(this.transloco.translate('tenants.payment.periodRequired')); return; }
    if (f.periodEnd <= f.periodStart) { this.notify.warn(this.transloco.translate('tenants.payment.periodOrder')); return; }
    if (f.amount <= 0) { this.notify.warn(this.transloco.translate('tenants.payment.amountRequired')); return; }

    const wasSuspendedForNonPayment =
      t.subscriptionStatus === SubscriptionStatus.Suspended && t.suspendReason === 'NonPayment';

    this.saving.set(true);
    this.service.createPayment(t.id, {
      periodStart: utcDateOnly(f.periodStart), periodEnd: utcDateOnly(f.periodEnd),
      amount: f.amount, currency: f.currency, method: f.method, note: f.note.trim() || null
    }).subscribe({
      next: () => {
        this.saving.set(false); this.paymentVisible.set(false);
        this.notify.success(this.transloco.translate('tenants.payment.paidUntilToast', { date: this.formatDate(f.periodEnd!) }));
        if (wasSuspendedForNonPayment) this.notify.info(this.transloco.translate('tenants.payment.reactivated'));
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
      this.transloco.translate('tenants.payment.cancelConfirm'),
      () => {
        this.service.deletePayment(p.id).subscribe(() => {
          this.notify.success(this.transloco.translate('tenants.payment.cancelled'));
          this.payments.update(list => list.filter(x => x.id !== p.id));
          this.load();
        });
      }
    );
  }

  // ---- Features -----------------------------------------------------------

  featuresVisible = signal(false);
  featuresTenant = signal<Tenant | null>(null);
  features = signal<TenantFeature[]>([]);
  loadingFeatures = signal(false);
  featureSearch = signal('');
  /** Qaysi guruh ochiq — akkordeon holati. */
  openGroups = signal<Set<string>>(new Set());

  /** Modul o'chiq bo'lsa uning feature'lari kuchga kirmaydi — buni UI'da ko'rsatamiz. */
  private enabledModuleCodes = signal<string[]>([]);

  /** Maxsus fitchalar alohida ko'rinsin — ular bitta mijoz uchun yozilgan. */
  customOnly = signal(false);

  featureGroups = computed(() => {
    const q = this.featureSearch().trim().toLowerCase();
    const onlyCustom = this.customOnly();
    const groups = new Map<string, TenantFeature[]>();
    for (const f of this.features()) {
      if (onlyCustom && !this.isCustomFeature(f)) continue;
      if (q && !f.code.toLowerCase().includes(q) && !f.name.toLowerCase().includes(q)) continue;
      // Modulga tegishli bo'lmagan feature'lar (export, import, analitika) alohida
      // guruhda — ular hech qanday modul bilan o'chirilmaydi.
      const key = f.moduleCode ?? GENERAL_GROUP;
      const list = groups.get(key);
      if (list) list.push(f); else groups.set(key, [f]);
    }
    return [...groups.entries()]
      .map(([moduleCode, items]) => ({
        moduleCode,
        items,
        moduleEnabled: moduleCode === GENERAL_GROUP || this.enabledModuleCodes().includes(moduleCode)
      }))
      .sort((a, b) => a.moduleCode.localeCompare(b.moduleCode));
  });

  openFeatures(t: Tenant) {
    this.featuresTenant.set(t);
    this.features.set([]);
    this.featureSearch.set('');
    this.featuresVisible.set(true);
    this.loadingFeatures.set(true);
    this.service.getTenantFeatures(t.id).subscribe({
      next: (r) => { this.features.set(r.success && r.data ? r.data : []); this.loadingFeatures.set(false); },
      error: () => this.loadingFeatures.set(false)
    });
    // Modul holati alohida keladi — guruhni kulrang qilish uchun kerak
    this.service.getTenantModules(t.id).subscribe(r => {
      if (r.success && r.data) this.enabledModuleCodes.set(r.data.filter(m => m.isEnabled).map(m => m.moduleCode));
    });
  }

  toggleGroup(moduleCode: string) {
    this.openGroups.update(set => {
      const next = new Set(set);
      next.has(moduleCode) ? next.delete(moduleCode) : next.add(moduleCode);
      return next;
    });
  }

  isGroupOpen(moduleCode: string) { return this.openGroups().has(moduleCode); }

  /** Uch holat: plan (override yo'q) · on · off. */
  featureState(f: TenantFeature): 'plan' | 'on' | 'off' {
    if (f.source !== 'tenant') return 'plan';
    return f.isEnabled ? 'on' : 'off';
  }

  /** Plan yoki katalog qiymatidan farq qilsa — bu qo'lda qo'yilgan. */
  isFeatureOutsidePlan(f: TenantFeature): boolean {
    return f.source === 'tenant';
  }

  isCustomFeature(f: TenantFeature): boolean {
    return f.isCustom === true || f.code.startsWith('custom.');
  }

  /** Boshqa mijoz uchun yozilgan fitchani yoqish — ongli qaror bo'lishi kerak. */
  private isForeignCustom(f: TenantFeature): boolean {
    const t = this.featuresTenant();
    return this.isCustomFeature(f) && !!f.ownerTenantId && !!t && f.ownerTenantId !== t.id;
  }

  setFeatureState(f: TenantFeature, state: 'plan' | 'on' | 'off') {
    if (state === 'on' && this.isForeignCustom(f)) {
      this.notify.confirmAction(
        this.transloco.translate('tenants.features.customConfirm', {
          name: f.name,
          owner: f.ownerTenantName ?? this.transloco.translate('tenants.features.unknownClient')
        }),
        this.transloco.translate('tenants.features.customConfirmHeader'),
        () => this.applyFeatureState(f, state)
      );
      return;
    }
    this.applyFeatureState(f, state);
  }

  private applyFeatureState(f: TenantFeature, state: 'plan' | 'on' | 'off') {
    const t = this.featuresTenant(); if (!t) return;
    const isEnabled = state === 'plan' ? null : state === 'on';
    this.service.updateTenantFeatures(t.id, {
      features: [{ code: f.code, isEnabled, note: f.note }]
    }).subscribe(() => {
      const key = state === 'plan' ? 'followsPlanToast' : state === 'on' ? 'enabledToast' : 'disabledToast';
      this.notify.success(this.transloco.translate('tenants.features.' + key, { name: f.name }));
      this.reloadFeatures();
    });
  }

  resetAllFeatures() {
    const t = this.featuresTenant(); if (!t) return;
    const overridden = this.features().filter(f => f.source === 'tenant');
    if (overridden.length === 0) { this.notify.info(this.transloco.translate('tenants.features.noOverrides')); return; }
    this.notify.confirmDelete(
      this.transloco.translate('tenants.features.resetConfirm', { count: overridden.length }),
      () => {
        this.service.updateTenantFeatures(t.id, {
          features: overridden.map(f => ({ code: f.code, isEnabled: null, note: null }))
        }).subscribe(() => {
          this.notify.success(this.transloco.translate('tenants.features.allFollowPlan'));
          this.reloadFeatures();
        });
      }
    );
  }

  private reloadFeatures() {
    const t = this.featuresTenant(); if (!t) return;
    this.service.getTenantFeatures(t.id).subscribe(r => {
      if (r.success && r.data) this.features.set(r.data);
    });
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
    return days < 0
      ? this.transloco.translate('tenants.daysOverdue', { days: -days })
      : this.transloco.translate('tenants.daysLeft', { days });
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
    return days < 0
      ? this.transloco.translate('tenants.expired')
      : this.transloco.translate('tenants.daysLeft', { days });
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
  statusLabel(s: SubscriptionStatus) {
    const key = SubscriptionStatus[s];
    return key ? this.transloco.translate('status.' + key) : '—';
  }
  statusClass(s: SubscriptionStatus) {
    return s === SubscriptionStatus.Active ? 'pill pill-success' : s === SubscriptionStatus.Trial ? 'pill pill-warning' : 'pill pill-danger';
  }
}
