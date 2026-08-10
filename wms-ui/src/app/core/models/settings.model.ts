export interface UserDetail {
  id: number;
  tenantId: number;
  fullName: string;
  phone: string;
  isActive: boolean;
  roles: RoleInfo[];
  createdAt: string;
  /** Platforma hisobi — tenant admin unga parol tiklay olmaydi (backend 403). */
  isSuperAdmin?: boolean;
}

/**
 * `POST /api/users/{id}/reset-password` javobi. `newPassword` **faqat bir marta**
 * keladi — hech qayerda saqlanmaydi va qayta olib bo'lmaydi. Shuning uchun u toast'ga
 * ham, konsolga ham chiqarilmaydi; faqat dialog ichida ko'rsatiladi.
 */
export interface PasswordResetResult {
  userId: number;
  login: string;
  fullName: string;
  newPassword: string;
}

export interface UserCreateDto {
  fullName: string;
  phone: string;
  password: string;
  isActive: boolean;
}

export interface UserUpdateDto {
  fullName: string;
  phone: string;
  isActive: boolean;
}

export interface RoleInfo {
  id: number;
  name: string;
  description: string | null;
  createdAt: string;
}

export interface RoleCreateDto {
  name: string;
  description: string | null;
}

export interface ModuleInfo {
  moduleId: number;
  moduleName: string;
  moduleCode: string;
  isEnabled: boolean;
}

export interface QcParameter {
  id: number;
  name: string;
  unit: string | null;
  minValue: number | null;
  maxValue: number | null;
  valueType: QcParameterType;
  createdAt: string;
}

export enum QcParameterType {
  Numeric = 1,
  Text = 2,
  Boolean = 3
}

export interface QcParameterCreateDto {
  name: string;
  unit: string | null;
  minValue: number | null;
  maxValue: number | null;
  valueType: QcParameterType;
}

export interface ChangePasswordDto {
  currentPassword: string;
  newPassword: string;
}

export interface PortalCounterparty {
  id: number;
  name: string;
  type: string;
  phone: string | null;
}

export interface PortalLoginDto {
  phone: string;
  password: string;
}

export interface PortalAuthResponse {
  token: string;
  counterparty: PortalCounterparty;
}

export interface PortalFinance {
  balance: number;
  totalDebt: number;
}

export interface PermissionInfo {
  id: number;
  code: string;
  name: string;
  module: string;
  /**
   * Ruxsat tenantning tarifida amalda ishlaydimi. `false` bo'lsa ham ro'yxatdan
   * **yashirilmaydi** — kulrang qilib ko'rsatiladi: yashirish sababni ham yashiradi
   * va admin "ruxsat berdim, ishlamayapti" deb qo'ng'iroq qiladi.
   */
  isAvailable?: boolean;
}
