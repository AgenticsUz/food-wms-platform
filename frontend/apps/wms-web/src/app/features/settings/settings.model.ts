/**
 * Sozlamalar bo'limi modeli — backend `DTOs/Users/UserDtos.cs`, `DTOs/Qc/QcDtos.cs`,
 * `DTOs/Audit/AuditDtos.cs`, `DTOs/Notifications` bilan bir xil. Id'lar Guid satr.
 *
 * F6 (D5, D7): foydalanuvchi WMS'da YARATILMAYDI va paroli yo'q — `UserCreateDto`,
 * `ChangePasswordDto`, `PasswordResetResult` o'chdi. Ruxsatlar endi id bilan emas,
 * KOD bilan yuradi (katalog kodda — `WmsPermissions`).
 */

export interface UserRole {
  readonly id: string;
  /** Tizim roli kodi (`admin`, `manager` …) yoki `null` (tenant yaratgan rol). */
  readonly code: string | null;
  readonly name: string;
}

export interface UserDetail {
  /** `user_profile.id` — WMS jadvallaridagi `*UserId` shunga ishora qiladi. */
  readonly id: string;
  /** Identity `sub`. */
  readonly identitySub: string;
  readonly fullName: string;
  readonly phone: string | null;
  readonly isActive: boolean;
  readonly lastSeenAt: string | null;
  readonly roles: readonly UserRole[];
}

/** `PUT /api/users/{id}` — faqat WMS o'chirgichi (ism/telefon Identity'niki). */
export interface UserUpdateDto {
  readonly isActive: boolean;
}

export interface RoleInfo {
  readonly id: string;
  readonly code: string | null;
  readonly name: string;
  readonly description: string | null;
  /** Tizim roli: tahrirlanadi, lekin o'chirilmaydi (JIT odamlarni shunga biriktiradi). */
  readonly isSystem: boolean;
  readonly userCount: number;
  /** Ruxsat KODLARI. */
  readonly permissions: readonly string[];
}

export interface RoleCreateDto {
  readonly name: string;
  readonly description: string | null;
  readonly permissionCodes: readonly string[];
}

export interface RoleUpdateDto {
  readonly name: string;
  readonly description: string | null;
}

export interface PermissionInfo {
  readonly code: string;
  readonly name: string;
  /** Guruh (`WAREHOUSE`, `SETTINGS` …) — UI sarlavhasi. */
  readonly module: string;
  /**
   * Ruxsat tenantning tarifida amalda ishlaydimi. `false` bo'lsa ham ro'yxatdan
   * yashirilmaydi — kulrang ko'rsatiladi: yashirish sababni ham yashiradi.
   */
  readonly isAvailable: boolean;
}

export enum QcParameterType {
  Numeric = 1,
  Text = 2,
  Boolean = 3,
}

export interface QcParameter {
  readonly id: string;
  readonly name: string;
  readonly unit: string | null;
  readonly minValue: number | null;
  readonly maxValue: number | null;
  readonly valueType: QcParameterType;
}

export interface QcParameterCreateDto {
  readonly name: string;
  readonly unit: string | null;
  readonly minValue: number | null;
  readonly maxValue: number | null;
  readonly valueType: QcParameterType;
}

/**
 * Audit jurnali qatori. F6: `userId` (int) o'rniga `actorSub` — jurnal Console
 * operatorini ham yozadi, uning esa bu tenantda profili yo'q. `entityId` — satr.
 */
export interface AuditLog {
  readonly id: string;
  readonly actorSub: string | null;
  readonly userName: string | null;
  /** HTTP metodi: `POST | PUT | DELETE | PATCH`. */
  readonly action: string;
  readonly entityType: string;
  readonly entityAction: string | null;
  readonly entityId: string | null;
  readonly path: string;
  readonly statusCode: number;
  readonly createdAt: string;
  readonly isPlatformAction: boolean;
}

/** `GET /api/me/telegram` — ulanish holati (TG1: deep-link, chat ID maydoni yo'q). */
export interface TelegramStatus {
  /** Bot sozlanganmi — sozlanmagan bo'lsa ulash tugmasi ko'rsatilmaydi. */
  readonly enabled: boolean;
  /** `@` siz; bot o'chiq bo'lsa `null`. */
  readonly botUsername: string | null;
  readonly linked: boolean;
  readonly linkedAt: string | null;
  /** Ulangan Telegram akkauntining username'i. */
  readonly username: string | null;
  /** O'chirilgan bildirishnoma turlari (TG3). */
  readonly mutedTypes: readonly string[];
}

/** `POST /api/me/telegram/link-token` — bir martalik havola. */
export interface TelegramLinkToken {
  readonly url: string;
  readonly expiresAt: string;
}
