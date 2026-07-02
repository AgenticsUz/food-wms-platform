import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap, switchMap, map, catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginDto, AuthResponse, User } from '../models/auth.model';
import { ApiResponse } from '../models/api-response.model';
import { PermissionService } from './permission.service';
import { NotificationBellService } from './notification-bell.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private permissionService = inject(PermissionService);
  private bellService = inject(NotificationBellService);

  token = signal<string | null>(localStorage.getItem('token'));
  currentUser = signal<User | null>(this.loadUserFromStorage());

  private loadUserFromStorage(): User | null {
    const stored = localStorage.getItem('currentUser');
    if (stored) {
      try { return JSON.parse(stored); } catch { return null; }
    }
    return null;
  }

  login(credentials: LoginDto) {
    return this.http.post<ApiResponse<AuthResponse>>(`${environment.apiUrl}/auth/login`, credentials).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.token.set(res.data.token);
          this.currentUser.set(res.data.user);
          localStorage.setItem('token', res.data.token);
          localStorage.setItem('currentUser', JSON.stringify(res.data.user));
          this.permissionService.setRoles(res.data.user.roles ?? []);
          localStorage.setItem('userRoles', JSON.stringify(res.data.user.roles ?? []));
        }
      }),
      // Permissionlarni yuklaymiz, LEKIN bu so'rov xato bersa ham login muvaffaqiyatli qoladi
      // (token allaqachon saqlangan; guard qayta urinadi). Har doim asl auth javobini qaytaramiz.
      switchMap(res => {
        if (res.success && res.data) {
          return this.refreshPermissions().pipe(
            catchError(() => of(null)),
            map(() => res)
          );
        }
        return of(res);
      })
    );
  }

  refreshPermissions() {
    return this.http.get<ApiResponse<string[]>>(`${environment.apiUrl}/auth/my-permissions`).pipe(
      tap(res => {
        if (res.success && res.data) {
          this.permissionService.setPermissions(res.data);
          localStorage.setItem('permissions', JSON.stringify(res.data));
        }
      })
    );
  }

  logout() {
    // 60s notification polling'ni to'xtatamiz — aks holda tokensiz 401 → logout sikli
    this.bellService.stopPolling();
    this.token.set(null);
    this.currentUser.set(null);
    this.permissionService.clearPermissions();
    localStorage.removeItem('token');
    localStorage.removeItem('currentUser');
    // Modul ro'yxatini ham tozalaymiz — boshqa tenant/user kirsa eski sidebar ko'rinmasin
    localStorage.removeItem('enabledModules');
    this.router.navigate(['/auth/login']);
  }

  isAuthenticated(): boolean {
    return !!this.token();
  }
}
