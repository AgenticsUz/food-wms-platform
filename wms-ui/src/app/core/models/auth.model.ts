export interface LoginDto {
  phone: string;
  password: string;
  tenantSlug: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface User {
  id: number;
  tenantId: number;
  tenantName?: string;
  fullName: string;
  phone: string;
  isActive: boolean;
  isSuperAdmin?: boolean;
  telegramChatId?: string | null;
  roles: string[];
  permissions?: string[];
  enabledModules?: string[];
  enabledFeatures?: string[];
}

