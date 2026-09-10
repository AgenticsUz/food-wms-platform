import { Injectable, inject } from '@angular/core';

import { ApiService } from '../../core/api/api.service';

/**
 * Transfer formasi uchun tanlov ro'yxatlari — kontragentlar va agentlar.
 *
 * Nega `features/counterparties` / `features/agents` servislaridan emas: ular
 * boshqa bo'limning to'liq CRUD modeli, bu yerga esa faqat tanlovga kerakli
 * 3–4 maydon kerak. O'z tor ko'rinishimiz bo'lsa, u bo'limlar o'z modelini
 * o'zgartirganda transfer formasi sinmaydi.
 *
 * Maydonlar backend `CounterpartyDto` / `AgentDto` dan (JSON'da ortiqcha
 * maydonlar ham keladi — ular bu yerda e'tiborsiz).
 */

export enum CounterpartyType {
  Supplier = 1,
  Client = 2,
  Both = 3,
}

export interface CounterpartyOption {
  readonly id: string;
  readonly name: string;
  readonly type: CounterpartyType;
  readonly agentId: string | null;
}

export interface AgentOption {
  readonly id: string;
  readonly name: string;
  readonly commissionPercent: number;
  readonly isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class TransferLookupsService {
  private readonly api = inject(ApiService);

  getCounterparties() {
    return this.api.get<CounterpartyOption[]>('counterparties');
  }

  getAgents() {
    return this.api.get<AgentOption[]>('agents');
  }
}
