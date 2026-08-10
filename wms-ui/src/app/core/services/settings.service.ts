import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import {
  UserDetail, UserCreateDto, UserUpdateDto,
  RoleInfo, RoleCreateDto,
  ModuleInfo,
  QcParameter, QcParameterCreateDto,
  ChangePasswordDto,
  PermissionInfo,
  PasswordResetResult
} from '../models/settings.model';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private api = inject(ApiService);

  // Users
  getUsers() { return this.api.get<UserDetail[]>('users'); }
  createUser(dto: UserCreateDto) { return this.api.post<UserDetail>('users', dto); }
  updateUser(id: number, dto: UserUpdateDto) { return this.api.put<UserDetail>(`users/${id}`, dto); }
  deleteUser(id: number) { return this.api.delete<void>(`users/${id}`); }
  assignRoles(userId: number, roleIds: number[]) { return this.api.put<void>(`users/${userId}/roles`, { roleIds }); }
  /**
   * Xodimning parolini tiklaydi. `newPassword` bermasa server 12 belgili parol yaratadi
   * (chalkash belgilarsiz — telefonda aytish uchun). Javobdagi parol **bir martalik**.
   * Tiklash xodimning barcha faol sessiyalarini bekor qiladi.
   */
  resetUserPassword(userId: number, newPassword: string | null) {
    return this.api.post<PasswordResetResult>(`users/${userId}/reset-password`, { newPassword });
  }

  // Roles
  getRoles() { return this.api.get<RoleInfo[]>('roles'); }
  createRole(dto: RoleCreateDto) { return this.api.post<RoleInfo>('roles', dto); }
  updateRole(id: number, dto: RoleCreateDto) { return this.api.put<RoleInfo>(`roles/${id}`, dto); }
  deleteRole(id: number) { return this.api.delete<void>(`roles/${id}`); }

  // Permissions
  getPermissions() { return this.api.get<PermissionInfo[]>('permissions'); }
  getRolePermissions(roleId: number) { return this.api.get<PermissionInfo[]>(`roles/${roleId}/permissions`); }
  updateRolePermissions(roleId: number, permissionIds: number[]) {
    return this.api.put<void>(`roles/${roleId}/permissions`, { permissionIds });
  }

  // Modules
  getModules(tenantId: number) { return this.api.get<ModuleInfo[]>(`tenants/${tenantId}/modules`); }
  toggleModules(tenantId: number, modules: { moduleId: number; isEnabled: boolean }[]) {
    return this.api.put<void>(`tenants/${tenantId}/modules`, { modules });
  }

  // QC Parameters
  getQcParameters() { return this.api.get<QcParameter[]>('qc/parameters'); }
  createQcParameter(dto: QcParameterCreateDto) { return this.api.post<QcParameter>('qc/parameters', dto); }
  updateQcParameter(id: number, dto: QcParameterCreateDto) { return this.api.put<QcParameter>(`qc/parameters/${id}`, dto); }
  deleteQcParameter(id: number) { return this.api.delete<void>(`qc/parameters/${id}`); }

  // Profile
  changePassword(dto: ChangePasswordDto) { return this.api.put<void>('auth/change-password', dto); }
  updateProfile(dto: { fullName: string; phone: string }) { return this.api.put<void>('auth/profile', dto); }
  setTelegram(chatId: string | null) { return this.api.put<void>('auth/telegram', { chatId }); }
}
