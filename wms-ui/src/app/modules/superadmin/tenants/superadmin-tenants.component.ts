import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { Select } from 'primeng/select';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { NotificationService } from '../../../shared/services/notification.service';
import { SuperAdminService } from '../../../core/services/superadmin.service';
import { Tenant, TenantModuleInfo, SubscriptionStatus } from '../../../core/models/tenant.model';

@Component({
  selector: 'app-superadmin-tenants',
  standalone: true,
  imports: [
    DatePipe, FormsModule, TranslocoDirective, TableModule, Button, Dialog,
    InputText, Password, Select, ToggleSwitch, PageHeaderComponent, StatusBadgeComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './superadmin-tenants.component.html',
  styleUrl: './superadmin-tenants.component.scss'
})
export default class SuperAdminTenantsComponent implements OnInit {
  private service = inject(SuperAdminService);
  private notify = inject(NotificationService);
  private transloco = inject(TranslocoService);

  tenants = signal<Tenant[]>([]);
  loading = signal(true);
  saving = signal(false);

  // Create / edit dialog
  dialogVisible = signal(false);
  editing = signal(false);
  form = signal<{
    id?: number; name: string; slug: string;
    adminFullName: string; adminPhone: string; adminPassword: string;
    isActive: boolean; planType: string | null; subscriptionStatus: SubscriptionStatus;
  }>(this.emptyForm());

  // Modules dialog
  modulesVisible = signal(false);
  modulesTenant = signal<Tenant | null>(null);
  modules = signal<TenantModuleInfo[]>([]);

  statusOptions = [
    { label: 'Trial', value: SubscriptionStatus.Trial },
    { label: 'Active', value: SubscriptionStatus.Active },
    { label: 'Suspended', value: SubscriptionStatus.Suspended }
  ];

  ngOnInit() { this.load(); }

  private emptyForm() {
    return {
      name: '', slug: '', adminFullName: '', adminPhone: '', adminPassword: '',
      isActive: true, planType: 'trial', subscriptionStatus: SubscriptionStatus.Trial
    };
  }

  load() {
    this.loading.set(true);
    this.service.getTenants().subscribe({
      next: (res) => { this.tenants.set(res.success && res.data ? res.data : []); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  openNew() {
    this.form.set(this.emptyForm());
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(t: Tenant) {
    this.form.set({
      id: t.id, name: t.name, slug: t.slug,
      adminFullName: '', adminPhone: '', adminPassword: '',
      isActive: t.isActive, planType: t.planType, subscriptionStatus: t.subscriptionStatus
    });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }

  save() {
    const f = this.form();
    if (!f.name.trim() || !f.slug.trim()) {
      this.notify.warn(this.transloco.translate('auth.fillAllFields'));
      return;
    }

    this.saving.set(true);
    if (this.editing()) {
      this.service.updateTenant(f.id!, {
        name: f.name.trim(), slug: f.slug.trim(), isActive: f.isActive,
        planType: f.planType, subscriptionStatus: f.subscriptionStatus
      }).subscribe({
        next: () => { this.afterSave(); },
        error: () => this.saving.set(false)
      });
    } else {
      if (!f.adminFullName.trim() || !f.adminPhone.trim() || f.adminPassword.length < 6) {
        this.saving.set(false);
        this.notify.warn(this.transloco.translate('superadmin.adminRequired'));
        return;
      }
      this.service.createTenant({
        name: f.name.trim(), slug: f.slug.trim(),
        adminFullName: f.adminFullName.trim(),
        adminPhone: f.adminPhone.trim(),
        adminPassword: f.adminPassword
      }).subscribe({
        next: () => { this.afterSave(); },
        error: () => this.saving.set(false)
      });
    }
  }

  private afterSave() {
    this.saving.set(false);
    this.dialogVisible.set(false);
    this.notify.success(this.transloco.translate('common.success'));
    this.load();
  }

  remove(t: Tenant) {
    this.notify.confirmDelete(`${t.name}?`, () => {
      this.service.deleteTenant(t.id).subscribe({
        next: () => { this.notify.success(this.transloco.translate('common.success')); this.load(); }
      });
    });
  }

  openModules(t: Tenant) {
    this.modulesTenant.set(t);
    this.modules.set([]);
    this.modulesVisible.set(true);
    this.service.getModules(t.id).subscribe({
      next: (res) => { if (res.success && res.data) this.modules.set(res.data); }
    });
  }

  toggleModule(m: TenantModuleInfo, enabled: boolean) {
    const t = this.modulesTenant();
    if (!t) return;
    this.service.toggleModule(t.id, m.moduleId, enabled).subscribe({
      next: () => {
        this.modules.update(list => list.map(x => x.moduleId === m.moduleId ? { ...x, isEnabled: enabled } : x));
      }
    });
  }

  statusClass(status: SubscriptionStatus): string {
    switch (status) {
      case SubscriptionStatus.Active: return 'Confirmed';
      case SubscriptionStatus.Trial: return 'Pending';
      case SubscriptionStatus.Suspended: return 'Cancelled';
      default: return 'Neutral';
    }
  }

  statusLabel(status: SubscriptionStatus): string {
    return SubscriptionStatus[status] ?? '—';
  }
}
