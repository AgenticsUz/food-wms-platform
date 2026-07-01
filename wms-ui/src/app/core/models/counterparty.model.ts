export interface Counterparty {
  id: number;
  name: string;
  type: CounterpartyType;
  phone: string | null;
  address: string | null;
  note: string | null;
  portalPhone: string | null;
  portalEnabled: boolean;
  agentId: number | null;
  agentName?: string | null;
  createdAt: string;
  updatedAt: string;
}

export enum CounterpartyType {
  Supplier = 1,
  Client = 2,
  Both = 3
}

export interface CounterpartyCreateDto {
  name: string;
  type: CounterpartyType;
  phone: string | null;
  address: string | null;
  note: string | null;
  portalPhone: string | null;
  portalEnabled: boolean;
  agentId?: number | null;
}

export interface CounterpartyBalance {
  counterpartyId: number;
  counterpartyName: string;
  debtAmount: number;
}

export interface PaymentHistory {
  id: number;
  counterpartyId: number;
  counterpartyName: string;
  transferId: number | null;
  amount: number;
  method: PaymentMethod;
  paidAt: string;
  note: string | null;
}

export enum PaymentMethod {
  Cash = 1,
  Bank = 2,
  Card = 3
}
