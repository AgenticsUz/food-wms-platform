import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { LoginDto, AuthResponse, AdminUser } from '../models/auth.model';

const TOKEN_KEY = 'adminToken';
const USER_KEY = 'adminUser';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  token = signal<string | null>(this.loadToken());
  currentUser = signal<AdminUser | null>(this.loadUser());
  isAuthenticated = computed(() =>
    !!this.token() && !!this.currentUser()?.isSuperAdmin && !this.isTokenExpired(this.token()!)
  );

  private loadUser(): AdminUser | null {
    const s = localStorage.getItem(USER_KEY);
    if (s) { try { return JSON.parse(s); } catch { return null; } }
    return null;
  }

  /**
   * Muddati o'tgan token bilan UI ochilmasin — birinchi 401 ni kutmasdan
   * startupda tozalanadi.
   */
  private loadToken(): string | null {
    const token = localStorage.getItem(TOKEN_KEY);
    if (token && this.isTokenExpired(token)) {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
      return null;
    }
    return token;
  }

  /** JWT `exp` (soniyalarda). Payload o'qilmasa — tokenni yaroqsiz deb hisoblaymiz. */
  private isTokenExpired(token: string): boolean {
    const parts = token.split('.');
    if (parts.length !== 3) return true;
    try {
      const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
      if (typeof payload.exp !== 'number') return false;
      return payload.exp * 1000 <= Date.now();
    } catch {
      return true;
    }
  }

  /** Faqat SuperAdmin kira oladi. Oddiy tenant admini rad etiladi. */
  login(credentials: LoginDto) {
    return this.http.post<ApiResponse<AuthResponse>>(`${environment.apiUrl}/auth/login`, credentials).pipe(
      map(res => {
        if (res.success && res.data && !res.data.user.isSuperAdmin) {
          return { success: false, data: null, message: 'This console is for platform administrators only.' } as ApiResponse<AuthResponse>;
        }
        return res;
      }),
      tap(res => {
        if (res.success && res.data) {
          this.token.set(res.data.token);
          this.currentUser.set(res.data.user);
          localStorage.setItem(TOKEN_KEY, res.data.token);
          localStorage.setItem(USER_KEY, JSON.stringify(res.data.user));
        }
      })
    );
  }

  logout() {
    this.token.set(null);
    this.currentUser.set(null);
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.router.navigate(['/login']);
  }
}
