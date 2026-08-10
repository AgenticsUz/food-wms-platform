import { Component, inject, signal, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { RadioButton } from 'primeng/radiobutton';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Password } from 'primeng/password';
import { Checkbox } from 'primeng/checkbox';
import { Tooltip } from 'primeng/tooltip';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { SettingsService } from '../../../core/services/settings.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { ImportButtonComponent } from '../../../shared/components/import-button/import-button.component';
import { PhoneInputComponent } from '../../../shared/components/phone-input/phone-input.component';
import { UserDetail, UserCreateDto, UserUpdateDto, RoleInfo, PasswordResetResult } from '../../../core/models/settings.model';
import { AuthService } from '../../../core/services/auth.service';
import { writeToClipboard } from '../../../shared/utils/clipboard.util';

/** Backenddagi `PasswordGenerator.MinimumManualLength` bilan bir xil. */
const MIN_PASSWORD_LENGTH = 8;

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [
    FormsModule, TableModule, Button, InputText, Dialog,
    ToggleSwitch, Password, Checkbox, Tooltip, RadioButton,
    PageHeaderComponent, StatusBadgeComponent, TranslocoDirective, ImportButtonComponent, PhoneInputComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export default class UsersComponent implements OnInit {
  private settingsService = inject(SettingsService);
  private notify = inject(NotificationService);
  private auth = inject(AuthService);
  private transloco = inject(TranslocoService);

  users = signal<UserDetail[]>([]);
  roles = signal<RoleInfo[]>([]);
  loading = signal(true);
  dialogVisible = signal(false);
  editing = signal(false);
  saving = signal(false);

  form = signal<UserCreateDto & { id?: number }>({
    fullName: '',
    phone: '',
    password: '',
    isActive: true
  });

  roleDialogVisible = signal(false);
  selectedUserId = signal<number | null>(null);
  selectedRoleIds = signal<number[]>([]);

  // ---- Parolni tiklash (F8) ------------------------------------------------
  // Yangi parol javobda bir marta keladi, qayta olib bo'lmaydi. Shuning uchun u
  // toast'ga ham, konsolga ham chiqarilmaydi va natija dialogi tasodifan yopilmaydi.

  resetTarget = signal<UserDetail | null>(null);
  resetMode = signal<'auto' | 'manual'>('auto');
  resetPassword = signal('');
  resetting = signal(false);
  resetResult = signal<PasswordResetResult | null>(null);
  copiedField = signal<'login' | 'password' | null>(null);

  readonly minPasswordLength = MIN_PASSWORD_LENGTH;

  /**
   * Forma va natija — ikki ALOHIDA dialog. PrimeNG `closable`/`closeOnEscape`
   * tinglovchilarini dialog ochilganda bir marta bog'laydi va keyin bu inputlar
   * o'zgarsa qayta ko'rib chiqmaydi; bitta dialogda bayroqlarni natija kelgach
   * `false` ga o'tkazish yetarli emas edi — Esc baribir yopib, qaytarib bo'lmaydigan
   * parolni yo'qotardi.
   */
  resetFormVisible = computed(() => this.resetTarget() !== null && this.resetResult() === null);

  /** O'ziga va platforma hisobiga tugma ko'rsatilmaydi — backend ham 403 beradi. */
  canReset(user: UserDetail): boolean {
    return user.id !== this.auth.currentUser()?.id && !user.isSuperAdmin;
  }

  manualTooShort = computed(() =>
    this.resetMode() === 'manual' &&
    this.resetPassword().trim().length > 0 &&
    this.resetPassword().trim().length < MIN_PASSWORD_LENGTH);

  canSubmitReset = computed(() =>
    this.resetMode() === 'auto' || this.resetPassword().trim().length >= MIN_PASSWORD_LENGTH);

  openReset(user: UserDetail) {
    this.resetTarget.set(user);
    this.resetMode.set('auto');
    this.resetPassword.set('');
    this.resetResult.set(null);
    this.copiedField.set(null);
  }

  /** Natija kelganda forma dialogi o'zi yopiladi — bu holatni tozalash deb hisoblamaymiz. */
  onResetFormVisibleChange(visible: boolean) {
    if (!visible && this.resetResult() === null) this.closeReset();
  }

  closeReset() {
    this.resetTarget.set(null);
    this.resetResult.set(null);
    this.resetPassword.set('');
    this.copiedField.set(null);
  }

  submitReset() {
    const user = this.resetTarget();
    if (!user || !this.canSubmitReset()) return;
    this.resetting.set(true);
    this.settingsService
      .resetUserPassword(user.id, this.resetMode() === 'manual' ? this.resetPassword().trim() : null)
      .subscribe({
        // Dialog ataylab ochiq qoladi: parol boshqa hech qayerdan olinmaydi.
        next: (res) => {
          this.resetting.set(false);
          if (res.success && res.data) this.resetResult.set(res.data);
        },
        // Xato toastini interceptor chiqaradi — ikkinchisini qo'shmaymiz.
        error: () => this.resetting.set(false)
      });
  }

  async copyValue(value: string, field: 'login' | 'password') {
    if (!await writeToClipboard(value)) {
      this.notify.warn(this.transloco.translate('settings.reset.copyFailed'));
      return;
    }
    this.copiedField.set(field);
    // Toast'da faqat "nusxalandi" — qiymatning o'zi hech qachon toastga tushmaydi.
    this.notify.success(this.transloco.translate('settings.reset.copied'));
    setTimeout(() => { if (this.copiedField() === field) this.copiedField.set(null); }, 2000);
  }

  ngOnInit() {
    this.loadUsers();
    this.loadRoles();
  }

  loadUsers() {
    this.loading.set(true);
    this.settingsService.getUsers().subscribe({
      next: (res) => {
        this.users.set(res.success && res.data ? res.data : []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.notify.error('Failed to load users');
      }
    });
  }

  loadRoles() {
    this.settingsService.getRoles().subscribe({
      next: (res) => {
        this.roles.set(res.success && res.data ? res.data : []);
      },
      error: () => this.notify.error('Failed to load roles')
    });
  }

  openNew() {
    this.form.set({ fullName: '', phone: '', password: '', isActive: true });
    this.editing.set(false);
    this.dialogVisible.set(true);
  }

  openEdit(user: UserDetail) {
    this.form.set({
      id: user.id,
      fullName: user.fullName,
      phone: user.phone,
      password: '',
      isActive: user.isActive
    });
    this.editing.set(true);
    this.dialogVisible.set(true);
  }

  save() {
    const f = this.form();
    if (!f.fullName.trim() || !f.phone) {
      this.notify.warn('Full name and phone are required');
      return;
    }
    if (!/^\+998\d{9}$/.test(f.phone)) {
      this.notify.warn('Telefon raqam 9 ta raqamdan iborat bo\'lishi kerak');
      return;
    }
    if (!this.editing() && !f.password.trim()) {
      this.notify.warn('Password is required for new users');
      return;
    }

    this.saving.set(true);

    if (this.editing()) {
      const dto: UserUpdateDto = {
        fullName: f.fullName.trim(),
        phone: f.phone,
        isActive: f.isActive
      };
      this.settingsService.updateUser(f.id!, dto).subscribe({
        next: () => {
          this.saving.set(false);
          this.dialogVisible.set(false);
          this.notify.success('User updated');
          this.loadUsers();
        },
        error: () => {
          this.saving.set(false);
          this.notify.error('Failed to update user');
        }
      });
    } else {
      const dto: UserCreateDto = {
        fullName: f.fullName.trim(),
        phone: f.phone,
        password: f.password,
        isActive: f.isActive
      };
      this.settingsService.createUser(dto).subscribe({
        next: () => {
          this.saving.set(false);
          this.dialogVisible.set(false);
          this.notify.success('User created');
          this.loadUsers();
        },
        error: () => {
          this.saving.set(false);
          this.notify.error('Failed to create user');
        }
      });
    }
  }

  deleteUser(user: UserDetail) {
    this.notify.confirmDelete(`Delete "${user.fullName}"?`, () => {
      this.settingsService.deleteUser(user.id).subscribe({
        next: () => {
          this.notify.success('User deleted');
          this.loadUsers();
        },
        error: () => this.notify.error('Failed to delete user')
      });
    });
  }

  toggleRole(roleId: number, checked: boolean) {
    this.selectedRoleIds.update(ids =>
      checked ? [...ids, roleId] : ids.filter(id => id !== roleId)
    );
  }

  openRoleDialog(user: UserDetail) {
    this.selectedUserId.set(user.id);
    this.selectedRoleIds.set(user.roles.map(r => r.id));
    this.roleDialogVisible.set(true);
  }

  saveRoles() {
    const userId = this.selectedUserId();
    if (!userId) return;

    this.saving.set(true);
    this.settingsService.assignRoles(userId, this.selectedRoleIds()).subscribe({
      next: () => {
        this.saving.set(false);
        this.roleDialogVisible.set(false);
        this.notify.success('Roles assigned');
        this.loadUsers();
      },
      error: () => {
        this.saving.set(false);
        this.notify.error('Failed to assign roles');
      }
    });
  }

  getRoleNames(user: UserDetail): string {
    return user.roles.map(r => r.name).join(', ') || '-';
  }

  updateForm(field: string, value: unknown) {
    this.form.update(f => ({ ...f, [field]: value }));
  }
}
