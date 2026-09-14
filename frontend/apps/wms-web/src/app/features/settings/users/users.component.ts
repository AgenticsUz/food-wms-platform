import { ChangeDetectionStrategy, Component, computed, inject, signal, type OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
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
 * O'CHGAN: yaratish, o'chirish, parol tiklash, Excel import — hisob Identity'da
 * ochiladi, profil birinchi kirishda JIT yoziladi. Ism va telefon Identity'niki
 * (JIT ularni har tokenda qayta yozadi) — shuning uchun tahrirlash dialogida
 * faqat WMS o'chirgichi (`isActive`) va rollar.
 *
 * F8.1 dan keyin hisobni Agentics EMAS, tenant adminining O'ZI ochadi
 * («Kirish hisoblari», `/settings/access`) — shuning uchun sarlavhada o'sha
 * ekranga tugma bor va ogohlantirish matni ham shuni aytadi.
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
  private readonly router = inject(Router);

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

  /**
   * «Kirish hisoblari» Identity yuzasi va u faqat `admin` rolini tan oladi
   * (`settings.routes.ts` dagi `roleGuard('admin')` bilan bir xil shart) —
   * ruxsat kodi emas.
   */
  readonly isAdmin = computed(() => this.session.hasRole('admin'));

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

  goToAccessAccounts(): void {
    void this.router.navigate(['/settings/access']);
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
    // Tizim roli ro'yxatda ko'rinmaydi — uni belgilab ham qo'ymaymiz, aks holda
    // «saqlash» uni yo'q rol sifatida yuborardi (server baribir saqlab qoladi).
    const sys = this.effectiveSystemRole(user);
    this.selectedRoleIds.set(user.roles.filter((r) => r.code === null || r.code !== sys).map((r) => r.id));
    this.roleTarget.set(user);
  }

  /**
   * Amaldagi tizim roli kodi. `identityRole` bo'sh — odam F8.1 dan keyin hali
   * kirmagan (T2, `docs/XATOLAR-2026-09-14.md` §9.3) — bo'lsa serverdagi
   * qoidani takrorlaymiz: profilda AYNAN bitta tizim roli (kodi `null` emas)
   * bo'lsa, o'sha amaldagi rol; aks holda noma'lum qoladi.
   */
  private effectiveSystemRole(user: UserDetail): string | null {
    if (user.identityRole) return user.identityRole;
    const systemCodes = user.roles.map((r) => r.code).filter((code): code is string => code !== null);
    return systemCodes.length === 1 ? systemCodes[0] : null;
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

  /**
   * Ekranda ikki toifa ajratiladi: Identity bergan TIZIM roli (o'qish uchun) va
   * tenant admini qo'shgan qo'shimcha rollar. Ilgari ikkalasi bitta ustunda
   * turardi va «Kirish hisoblari» da rol o'zgargach eskisi qolib ketardi.
   */
  customRoleNames(user: UserDetail): string {
    const sys = this.effectiveSystemRole(user);
    const names = user.roles.filter((r) => r.code === null || r.code !== sys).map((r) => r.name);
    return names.join(', ') || '—';
  }

  /** Tizim roli yorlig'i — token roli kodidan (`shell.roles.*`). */
  identityRoleLabel(user: UserDetail): string {
    const sys = this.effectiveSystemRole(user);
    return sys
      ? this.language.translate('shell.roles.' + sys)
      : this.language.translate('settings.identityRoleNone');
  }

  /** Rol dialogida tanlanadigan rollar — tizim roli bundan mustasno. */
  assignableRoles(user: UserDetail | null): readonly RoleInfo[] {
    const sys = user ? this.effectiveSystemRole(user) : null;
    return this.roles().filter((r) => !user || r.code === null || r.code !== sys);
  }

  parse(value: string | null): Date | null {
    return parseUtc(value);
  }
}
