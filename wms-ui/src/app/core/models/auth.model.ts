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
  fullName: string;
  phone: string;
  isActive: boolean;
  roles: string[];
}
