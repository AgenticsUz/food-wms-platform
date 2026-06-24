import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';
import {
  Agent, AgentCreateDto, AgentSalesReport, CommissionRecord, CommissionStatus, PayCommissionDto
} from '../models/agent.model';

@Injectable({ providedIn: 'root' })
export class AgentService {
  private api = inject(ApiService);

  getAgents() {
    return this.api.get<Agent[]>('agents');
  }

  getAgent(id: number) {
    return this.api.get<Agent>(`agents/${id}`);
  }

  createAgent(dto: AgentCreateDto) {
    return this.api.post<Agent>('agents', dto);
  }

  updateAgent(id: number, dto: AgentCreateDto) {
    return this.api.put<Agent>(`agents/${id}`, dto);
  }

  deleteAgent(id: number) {
    return this.api.delete<void>(`agents/${id}`);
  }

  getSales(id: number, params?: Record<string, string | number | boolean>) {
    return this.api.get<AgentSalesReport>(`agents/${id}/sales`, params);
  }

  getCommissions(id: number) {
    return this.api.get<CommissionRecord[]>(`agents/${id}/commissions`);
  }

  payCommission(id: number, dto: PayCommissionDto) {
    return this.api.post<void>(`agents/${id}/commissions/pay`, dto);
  }

  updateCommissionStatus(recordId: number, status: CommissionStatus) {
    return this.api.put<void>(`agents/commissions/${recordId}/status`, { status });
  }
}
