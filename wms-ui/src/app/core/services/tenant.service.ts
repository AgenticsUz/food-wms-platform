import { Injectable, inject, signal } from '@angular/core';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';

export interface TenantModule {
  moduleId: number;
  moduleName: string;
  moduleCode: string;
  isEnabled: boolean;
}

@Injectable({ providedIn: 'root' })
export class TenantService {
  private api = inject(ApiService);
  private authService = inject(AuthService);

  enabledModules = signal<string[]>([]);

  isModuleEnabled(code: string): boolean {
    return this.enabledModules().includes(code);
  }

  loadModules() {
    const user = this.authService.currentUser();
    if (!user) return;

    this.api.get<TenantModule[]>(`tenants/${user.tenantId}/modules`).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          const enabled = res.data
            .filter(m => m.isEnabled)
            .map(m => m.moduleCode);
          this.enabledModules.set(enabled);
          localStorage.setItem('enabledModules', JSON.stringify(enabled));
        }
      },
      error: () => {
        // Fallback: load from localStorage or enable all for dev
        const stored = localStorage.getItem('enabledModules');
        if (stored) {
          try {
            this.enabledModules.set(JSON.parse(stored));
          } catch {
            this.enableAllForDev();
          }
        } else {
          this.enableAllForDev();
        }
      }
    });
  }

  restoreModules() {
    const stored = localStorage.getItem('enabledModules');
    if (stored) {
      try {
        this.enabledModules.set(JSON.parse(stored));
      } catch {
        this.enableAllForDev();
      }
    } else {
      this.enableAllForDev();
    }
  }

  setModules(modules: string[]) {
    this.enabledModules.set(modules);
    localStorage.setItem('enabledModules', JSON.stringify(modules));
  }

  private enableAllForDev() {
    const all = [
      'WAREHOUSE_RAW', 'PRODUCTION', 'WAREHOUSE_FINISHED',
      'TRANSFERS', 'FINANCE', 'KPI', 'SUPPLIERS', 'CLIENTS', 'QUALITY'
    ];
    this.enabledModules.set(all);
  }
}
