export interface UserDetail {
  id: number;
  tenantId: number;
  fullName: string;
  phone: string;
  isActive: boolean;
  roles: RoleInfo[];
  createdAt: string;
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
