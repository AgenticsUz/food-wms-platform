import { Injectable, inject } from '@angular/core';

import { ApiService } from '../../core/api/api.service';
import type {
  PortalAgentClient,
  PortalAgentSummary,
  PortalFinance,
  PortalMe,
  PortalPayment,
  PortalTransfer,
} from './portal.model';

/**
 * Kabinet API'si (`/api/portal/*`).
 *
 * ⚠️ Hech bir metod kontragent yoki agent id'sini YUBORMAYDI: serverda u tokendan
 * yechiladi. Bu — yuzaning asosiy qoidasi, shuning uchun shu yerda ham parametr yo'q.
 */
@Injectable({ providedIn: 'root' })
export class PortalService {
  private readonly api = inject(ApiService);

  getMe() {
    return this.api.get<PortalMe>('portal/me');
  }

  getFinance() {
    return this.api.get<PortalFinance>('portal/finance');
  }

  getTransfers(page = 1, pageSize = 50) {
    return this.api.get<PortalTransfer[]>('portal/transfers', { page, pageSize });
  }

  getPayments() {
    return this.api.get<PortalPayment[]>('portal/payments');
  }

  getAgentSummary() {
    return this.api.get<PortalAgentSummary>('portal/agent/summary');
  }

  getAgentClients() {
    return this.api.get<PortalAgentClient[]>('portal/agent/clients');
  }
}
