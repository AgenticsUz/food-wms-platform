import { Injectable, inject } from '@angular/core';

import { ApiService, type ApiCallOptions, type QueryParams } from '../../core/api/api.service';
import type {
  AuditLog,
  PermissionInfo,
  QcParameter,
  QcParameterCreateDto,
  RoleCreateDto,
  RoleInfo,
  RoleUpdateDto,
  TelegramLinkToken,
  TelegramStatus,
  UserDetail,
  UserUpdateDto,
} from './settings.model';

/**
 * Sozlamalar API'si (eski `settings.service` + `audit.service`).
 *
 * O'CHGAN (D5, D7): `createUser`, `deleteUser`, `resetUserPassword`,
 * `changePassword`, `updateProfile` — hisob va parol Identity'da;
 * `getModules/toggleModules` — modullar `/api/me` dan (D6).
 */
@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly api = inject(ApiService);

  getUsers() {
    return this.api.get<UserDetail[]>('users');
  }
  updateUser(id: string, dto: UserUpdateDto) {
    return this.api.put<UserDetail>(`users/${id}`, dto);
  }
  assignRoles(userId: string, roleIds: readonly string[]) {
    return this.api.put<UserDetail>(`users/${userId}/roles`, { roleIds });
  }

  getRoles(options?: ApiCallOptions) {
    return this.api.get<RoleInfo[]>('roles', undefined, options);
  }
  createRole(dto: RoleCreateDto) {
    return this.api.post<RoleInfo>('roles', dto);
  }
  updateRole(id: string, dto: RoleUpdateDto) {
    return this.api.put<RoleInfo>(`roles/${id}`, dto);
  }
  deleteRole(id: string) {
    return this.api.delete<void>(`roles/${id}`);
  }

  getPermissions() {
    return this.api.get<PermissionInfo[]>('permissions');
  }
  getRolePermissions(roleId: string) {
    return this.api.get<PermissionInfo[]>(`roles/${roleId}/permissions`);
  }
  /** Ro'yxat rolning to'plamini ALMASHTIRADI. */
  updateRolePermissions(roleId: string, permissionCodes: readonly string[]) {
    return this.api.put<void>(`roles/${roleId}/permissions`, { permissionCodes });
  }

  getQcParameters() {
    return this.api.get<QcParameter[]>('qc/parameters');
  }
  createQcParameter(dto: QcParameterCreateDto) {
    return this.api.post<QcParameter>('qc/parameters', dto);
  }
  updateQcParameter(id: string, dto: QcParameterCreateDto) {
    return this.api.put<QcParameter>(`qc/parameters/${id}`, dto);
  }
  deleteQcParameter(id: string) {
    return this.api.delete<void>(`qc/parameters/${id}`);
  }

  getAuditLogs(params: QueryParams) {
    return this.api.get<AuditLog[]>('audit', params);
  }
  getAuditEntityTypes() {
    return this.api.get<string[]>('audit/entity-types');
  }

  getTelegram(options?: ApiCallOptions) {
    return this.api.get<TelegramStatus>('me/telegram', undefined, options);
  }
  /** Bir martalik deep-link; oldingi faol havola bekor bo'ladi. */
  createTelegramLink() {
    return this.api.post<TelegramLinkToken>('me/telegram/link-token', {});
  }
  unlinkTelegram() {
    return this.api.delete<void>('me/telegram');
  }
  /** O'chirilgan turlar — `NotificationType` nomlari; ro'yxat to'plamni ALMASHTIRADI. */
  setTelegramMuted(types: readonly string[]) {
    return this.api.put<void>('me/telegram/muted', { types });
  }
  setTelegramDigest(enabled: boolean) {
    return this.api.put<void>('me/telegram/digest', { enabled });
  }
}
