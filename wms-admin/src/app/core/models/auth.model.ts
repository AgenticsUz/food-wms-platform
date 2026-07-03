export interface LoginDto {
  phone: string;
  password: string;
  tenantSlug: string;
}

export interface AdminUser {
  id: number;
  fullName: string;
  phone: string;
  tenantId: number;
  tenantName: string;
  isSuperAdmin: boolean;
}

export interface AuthResponse {
  token: string;
  user: AdminUser;
}
