import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';
import { Button } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Tooltip } from 'primeng/tooltip';

import { WmsSession } from '../../../core/auth/wms-session';
import { NotificationService } from '../../../core/notify/notification.service';
import { parseUtc } from '../../../core/utils/date.util';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import type { RoleInfo, UserDetail } from '../settings.model';
import { SettingsService } from '../settings.service';

/**
 * Foydalanuvchilar — WMS'dagi qismi (D7).
 *
 * O'CHGAN: yaratish, o'chirish, parol tiklash, Excel import — hisobni Console
 * Identity'da ochadi va zavodga biriktiradi, profil birinchi kirishda JIT
 * yoziladi. Ism va telefon Identity'niki (JIT ularni har tokenda qayta yozadi) —
 * shuning uchun tahrirlash dialogida faqat WMS o'chirgichi (`isActive`) va rollar.
 */
@Component({
  selector: 'app-users',
  imports: [
    DatePipe,
    FormsModule,
    TableModule,
    Button,
    InputText,
    Dialog,
    ToggleSwitch,
    Checkbox,
    Tooltip,
    PageHeaderComponent,
    StatusBadgeComponent,
    TranslocoDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss',
})
export default class UsersComponent implements OnInit {
  private readonly settingsService = inject(SettingsService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);
  private readonly session = inject(WmsSession);

  readonly users = signal<UserDetail[]>([]);
  readonly roles = signal<RoleInfo[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);

  /** Holat dialogi — tanlangan foydalanuvchi va yangi `isActive`. */
  readonly editTarget = signal<UserDetail | null>(null);
  readonly editActive = signal(true);
  readonly dialogVisible = computed(() => this.editTarget() !== null);

  readonly roleTarget = signal<UserDetail | null>(null);
  readonly selectedRoleIds = signal<string[]>([]);
  readonly roleDialogVisible = computed(() => this.roleTarget() !== null);

  /** O'zini o'chirib qo'ysa, keyingi so'rovdan WMS'ga kira olmay qoladi. */
  readonly editingSelf = computed(() => this.editTarget()?.id === this.session.me()?.profileId);

  ngOnInit(): void {
    this.loadUsers();
    this.loadRoles();
  }

  loadUsers(): void {
    this.loading.set(true);
    this.settingsService.getUsers().subscribe({
      next: (res) => {
        this.users.set(res.success ? (res.data ?? []) : []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  /**
   * Rollar ro'yxati `settings.roles` ruxsatini talab qiladi, bu ekran esa
   * `settings.users` bilan ochiladi. Ruxsat bo'lmasa toast chiqmaydi — rol
   * dialogida «ma'lumot yo'q» ko'rinadi.
   */
  private loadRoles(): void {
    this.settingsService.getRoles({ skipErrorNotify: true }).subscribe({
      next: (res) => this.roles.set(res.success ? (res.data ?? []) : []),
      error: () => this.roles.set([]),
    });
  }

  openEdit(user: UserDetail): void {
    this.editActive.set(user.isActive);
    this.editTarget.set(user);
  }

  closeEdit(): void {
    this.editTarget.set(null);
  }

  save(): void {
    const user = this.editTarget();
    if (!user) return;
    this.saving.set(true);
    this.settingsService.updateUser(user.id, { isActive: this.editActive() }).subscribe({
      next: () => {
        this.saving.set(false);
        this.editTarget.set(null);
        this.notify.success(this.language.translate('common.success'));
        this.loadUsers();
      },
      error: () => this.saving.set(false),
    });
  }

  openRoleDialog(user: UserDetail): void {
    this.selectedRoleIds.set(user.roles.map((r) => r.id));
    this.roleTarget.set(user);
  }

  closeRoleDialog(): void {
    this.roleTarget.set(null);
  }

  toggleRole(roleId: string, checked: boolean): void {
    this.selectedRoleIds.update((ids) => (checked ? [...ids, roleId] : ids.filter((id) => id !== roleId)));
  }

  saveRoles(): void {
    const user = this.roleTarget();
    if (!user) return;
    this.saving.set(true);
    this.settingsService.assignRoles(user.id, this.selectedRoleIds()).subscribe({
      next: () => {
        this.saving.set(false);
        this.roleTarget.set(null);
        this.notify.success(this.language.translate('common.success'));
        this.loadUsers();
      },
      error: () => this.saving.set(false),
    });
  }

  roleNames(user: UserDetail): string {
    return user.roles.map((r) => r.name).join(', ') || '-';
  }

  parse(value: string | null): Date | null {
    return parseUtc(value);
  }
}
