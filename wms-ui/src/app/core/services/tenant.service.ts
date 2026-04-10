import { Injectable, inject, signal } from '@angular/core';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class TenantService {
  private api = inject(ApiService);

  enabledModules = signal<string[]>([]);

  isModuleEnabled(code: string): boolean {
    return this.enabledModules().includes(code);
  }

  loadModules() {
    this.api.get<string[]>('auth/me').subscribe(res => {
      // Will be properly wired when backend is ready
      // For now, enable all modules for development
    });
  }

  setModules(modules: string[]) {
    this.enabledModules.set(modules);
  }
}
