import { Injectable, inject, signal, isDevMode } from '@angular/core';
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
        // Xatoda faqat saqlangan ro'yxatga qaytamiz. Prod'da hech qachon
        // "hammasini yoqish" — aks holda o'chirilgan modul bir xatodan keyin ochilib qoladi.
        this.restoreModules();
      }
    });
  }

  restoreModules() {
    const stored = localStorage.getItem('enabledModules');
    if (stored) {
      try {
        this.enabledModules.set(JSON.parse(stored));
        return;
      } catch {
        // buzuq JSON — pastdagi fallback'ga tushamiz
      }
    }
    // Saqlangan ro'yxat yo'q: dev'da qulaylik uchun hammasini yoqamiz,
    // prod'da esa bo'sh qoldiramiz (loadModules() serverdan to'g'ri ro'yxatni oladi).
    this.enabledModules.set(isDevMode() ? this.allModules() : []);
  }

  setModules(modules: string[]) {
    this.enabledModules.set(modules);
    localStorage.setItem('enabledModules', JSON.stringify(modules));
  }

  clearModules() {
    this.enabledModules.set([]);
    localStorage.removeItem('enabledModules');
  }

  private allModules(): string[] {
    return [
      'WAREHOUSE_RAW', 'PRODUCTION', 'WAREHOUSE_FINISHED',
      'TRANSFERS', 'FINANCE', 'KPI', 'SUPPLIERS', 'CLIENTS', 'QUALITY'
    ];
  }
}
