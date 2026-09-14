import { Injectable, inject } from '@angular/core';

import { ApiService, type ApiCallOptions } from '../../core/api/api.service';
import type {
  Agent,
  AgentSalesReport,
  AgentSaveDto,
  CommissionRecord,
  CommissionStatus,
  PayCommissionDto,
} from './agent.model';

/** Agentlar API'si (eski `core/services/agent.service.ts`). */
@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly api = inject(ApiService);

  /** `options` — mijozlar formasi agentlarni ixtiyoriy tanlov sifatida jim o'qiydi. */
  getAgents(options?: ApiCallOptions) {
    return this.api.get<Agent[]>('agents', undefined, options);
  }
  /** Bitta agent — detal sahifasida telefon kerak (hisobot DTO'sida u yo'q). */
  getAgent(id: string) {
    return this.api.get<Agent>(`agents/${id}`);
  }
  createAgent(dto: AgentSaveDto) {
    return this.api.post<Agent>('agents', dto);
  }
  updateAgent(id: string, dto: AgentSaveDto) {
    return this.api.put<Agent>(`agents/${id}`, dto);
  }
  deleteAgent(id: string) {
    return this.api.delete<null>(`agents/${id}`);
  }
  getSales(id: string) {
    return this.api.get<AgentSalesReport>(`agents/${id}/sales`);
  }
  /**
   * Komissiya yozuvlari `agents.commissions` feature'i ortida. Hisobot (yuqoridagi
   * kartalar) undan qat'i nazar ochilsin — jadval bo'sh qoladi, toast chiqmaydi.
   */
  getCommissions(id: string) {
    return this.api.get<CommissionRecord[]>(`agents/${id}/commissions`, undefined, {
      skipErrorNotify: true,
    });
  }
  payCommission(id: string, dto: PayCommissionDto) {
    return this.api.post<null>(`agents/${id}/commissions/pay`, dto);
  }
  updateCommissionStatus(recordId: string, status: CommissionStatus) {
    return this.api.put<null>(`agents/commissions/${recordId}/status`, { status });
  }
}
