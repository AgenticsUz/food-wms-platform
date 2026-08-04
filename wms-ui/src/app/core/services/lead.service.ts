import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import { CreateLeadDto } from '../models/lead.model';

/**
 * Self-service registratsiya yopilgan: tenantni faqat platforma egasi yaratadi.
 * Bu servis mijozdan qo'ng'iroq uchun ma'lumot qoldiradi, hisob ochmaydi.
 */
@Injectable({ providedIn: 'root' })
export class LeadService {
  private api = inject(ApiService);

  requestDemo(dto: CreateLeadDto) {
    return this.api.post<void>('leads', dto);
  }
}
