import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { Password } from 'primeng/password';
import { MultiSelect } from 'primeng/multiselect';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';
import { SettingsService } from '../../../core/services/settings.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { UserDetail, UserCreateDto, UserUpdateDto, RoleInfo } from '../../../core/models/settings.model';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [
    FormsModule, TableModule, Button, InputText, Dialog,
    ToggleSwitch, Password, MultiSelect,
    PageHeaderComponent, StatusBadgeComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss'
})
export default class UsersComponent implements OnInit {
  private settingsService = inject(SettingsService);
  private notify = inject(NotificationService);

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
    if (!f.fullName.trim() || !f.phone.trim()) {
      this.notify.warn('Full name and phone are required');
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
        phone: f.phone.trim(),
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
        phone: f.phone.trim(),
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
