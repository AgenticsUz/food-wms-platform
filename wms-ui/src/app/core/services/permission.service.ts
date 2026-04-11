import { Injectable, inject, signal, computed } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class PermissionService {
  private permissions = signal<string[]>([]);
  private roles = signal<string[]>([]);

  private isAdmin = computed(() => this.roles().includes('Admin'));

  setPermissions(codes: string[]) {
    this.permissions.set(codes);
  }

  setRoles(userRoles: string[]) {
    this.roles.set(userRoles);
  }

  can(code: string): boolean {
    if (this.isAdmin()) return true;
    return this.permissions().includes(code);
  }

  canAny(...codes: string[]): boolean {
    if (this.isAdmin()) return true;
    return codes.some(c => this.permissions().includes(c));
  }

  restorePermissions() {
    const stored = localStorage.getItem('permissions');
    if (stored) {
      try { this.permissions.set(JSON.parse(stored)); } catch { this.permissions.set([]); }
    }
    const storedRoles = localStorage.getItem('userRoles');
    if (storedRoles) {
      try { this.roles.set(JSON.parse(storedRoles)); } catch { this.roles.set([]); }
    }
  }

  clearPermissions() {
    this.permissions.set([]);
    this.roles.set([]);
    localStorage.removeItem('permissions');
    localStorage.removeItem('userRoles');
  }
}
