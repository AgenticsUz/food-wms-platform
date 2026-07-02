import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import { AuditLog } from '../models/audit.model';

@Injectable({ providedIn: 'root' })
export class AuditService {
  private api = inject(ApiService);

  getLogs(params?: Record<string, string | number | boolean>) {
    return this.api.get<AuditLog[]>('audit', params);
  }

  getEntityTypes() {
    return this.api.get<string[]>('audit/entity-types');
  }
}
