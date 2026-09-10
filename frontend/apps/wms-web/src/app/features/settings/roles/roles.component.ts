import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { Textarea } from 'primeng/textarea';
import { Tooltip } from 'primeng/tooltip';

import { NotificationService } from '../../../core/notify/notification.service';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import type { PermissionInfo, RoleInfo } from '../settings.model';
import { SettingsService } from '../settings.service';

interface PermissionGroup {
  readonly module: string;
  readonly permissions: readonly (PermissionInfo & { readonly checked: boolean })[];
}

interface RoleForm {
  name: string;
  description: string | null;
}

/**
 * Rollar. F6: ruxsatlar KOD bilan yuradi (`permissionCodes`), katalog kodda.
 * Tizim rollari (`admin/manager/employee/viewer`) tahrirlanadi, lekin
 * o'chirilmaydi — JIT odamlarni Identity'dagi rol bo'yicha aynan shularga
 * biriktiradi, shuning uchun o'chirish tugmasi ko'rsatilmaydi.
 */
@Component({
  selector: 'app-roles',
  imports: [
    FormsModule,
    TableModule,
    Button,
    InputText,
    Dialog,
    Textarea,
    Checkbox,
    Tooltip,
    PageHeaderComponent,
    TranslocoDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './roles.component.html',
  styleUrl: './roles.component.scss',
})
export default class RolesComponent implements OnInit {
  private readonly settingsService = inject(SettingsService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  readonly roles = signal<RoleInfo[]>([]);
  readonly loading = signal(true);
  readonly dialogVisible = signal(false);
  readonly saving = signal(false);
  readonly editingId = signal<string | null>(null);

  readonly form = signal<RoleForm>({ name: '', description: null });

  readonly permRole = signal<RoleInfo | null>(null);
  readonly permDialogVisible = computed(() => this.permRole() !== null);
  readonly permLoading = signal(false);
  readonly permSaving = signal(false);
  readonly allPermissions = signal<PermissionInfo[]>([]);
  readonly selectedCodes = signal<ReadonlySet<string>>(new Set());

  readonly permissionGroups = computed<PermissionGroup[]>(() => {
    const selected = this.selectedCodes();
    const groups = new Map<string, (PermissionInfo & { checked: boolean })[]>();
    for (const p of this.allPermissions()) {
      const module = p.module || p.code.split('.')[0].toUpperCase();
      const list = groups.get(module) ?? [];
      list.push({ ...p, checked: selected.has(p.code) });
      groups.set(module, list);
    }
    return [...groups.entries()].map(([module, permissions]) => ({ module, permissions }));
  });

  ngOnInit(): void {
    this.loadRoles();
  }

  loadRoles(): void {
    this.loading.set(true);
    this.settingsService.getRoles().subscribe({
      next: (res) => {
        this.roles.set(res.success ? (res.data ?? []) : []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openNew(): void {
    this.form.set({ name: '', description: null });
    this.editingId.set(null);
    this.dialogVisible.set(true);
  }

  openEdit(role: RoleInfo): void {
    this.form.set({ name: role.name, description: role.description });
    this.editingId.set(role.id);
    this.dialogVisible.set(true);
  }

  save(): void {
    const f = this.form();
    if (!f.name.trim()) {
      this.notify.warn(this.language.translate('auth.fillAllFields'));
      return;
    }

    this.saving.set(true);
    const name = f.name.trim();
    const description = f.description?.trim() || null;
    const id = this.editingId();
    const request =
      id === null
        ? this.settingsService.createRole({ name, description, permissionCodes: [] })
        : this.settingsService.updateRole(id, { name, description });

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogVisible.set(false);
        this.notify.success(this.language.translate('common.success'));
        this.loadRoles();
      },
      error: () => this.saving.set(false),
    });
  }

  deleteRole(role: RoleInfo): void {
    this.notify.confirmDelete(`${role.name}?`, () => {
      this.settingsService.deleteRole(role.id).subscribe({
        next: () => {
          this.notify.success(this.language.translate('common.success'));
          this.loadRoles();
        },
      });
    });
  }

  openPermissions(role: RoleInfo): void {
    this.selectedCodes.set(new Set(role.permissions));
    this.permRole.set(role);
    this.permLoading.set(true);

    // Katalog + rolning JORIY to'plami (ro'yxatdagi `role.permissions` sahifa
    // ochilgandan beri eskirgan bo'lishi mumkin — boshqa admin o'zgartirgan).
    this.settingsService.getPermissions().subscribe({
      next: (res) => {
        if (res.success && res.data) this.allPermissions.set(res.data);
        this.settingsService.getRolePermissions(role.id).subscribe({
          next: (permRes) => {
            if (permRes.success && permRes.data) {
              this.selectedCodes.set(new Set(permRes.data.map((p) => p.code)));
            }
            this.permLoading.set(false);
          },
          error: () => this.permLoading.set(false),
        });
      },
      error: () => this.permLoading.set(false),
    });
  }

  closePermissions(): void {
    this.permRole.set(null);
  }

  togglePermission(code: string, checked: boolean): void {
    this.selectedCodes.update((codes) => {
      const next = new Set(codes);
      if (checked) next.add(code);
      else next.delete(code);
      return next;
    });
  }

  savePermissions(): void {
    const role = this.permRole();
    if (!role) return;

    this.permSaving.set(true);
    this.settingsService.updateRolePermissions(role.id, [...this.selectedCodes()]).subscribe({
      next: () => {
        this.permSaving.set(false);
        this.permRole.set(null);
        this.notify.success(this.language.translate('common.success'));
        this.loadRoles();
      },
      error: () => this.permSaving.set(false),
    });
  }

  updateForm<K extends keyof RoleForm>(field: K, value: RoleForm[K]): void {
    this.form.update((f) => ({ ...f, [field]: value }));
  }
}
