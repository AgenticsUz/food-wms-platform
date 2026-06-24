export interface Agent {
  id: number;
  name: string;
  phone: string | null;
  commissionPercent: number;
  isActive: boolean;
  portalEnabled: boolean;
  portalPhone: string | null;
  salesCount: number;
  totalSales: number;
  totalCommission: number;
  commissionPaid: number;
  commissionDue: number;
}

export interface AgentCreateDto {
  name: string;
  phone: string | null;
  commissionPercent: number;
  isActive: boolean;
  portalEnabled: boolean;
  portalPhone: string | null;
  portalPassword?: string | null;
}

export enum CommissionStatus {
  Pending = 1,
  Confirmed = 2,
  Cancelled = 3
}

export interface CommissionRecord {
  id: number;
  agentId: number;
  agentName: string;
  transferId: number;
  counterpartyName: string | null;
  saleAmount: number;
  commissionPercent: number;
  commissionAmount: number;
  status: CommissionStatus;
  isPaid: boolean;
  paidAt: string | null;
  createdAt: string;
}

export interface AgentSalesPoint {
  date: string;
  saleAmount: number;
  commissionAmount: number;
}

export interface AgentSalesReport {
  agentId: number;
  agentName: string;
  commissionPercent: number;
  salesCount: number;
  totalSales: number;
  totalCommission: number;
  commissionConfirmed: number;
  commissionPending: number;
  commissionCancelled: number;
  commissionPaid: number;
  commissionDue: number;
  timeline: AgentSalesPoint[];
}

export interface PayCommissionDto {
  amount: number;
  note: string | null;
  recordAsExpense: boolean;
}

// ── Agent portal (self-service cabinet) ──

export interface AgentPortalLoginDto {
  phone: string;
  password: string;
}

export interface AgentPortalProfile {
  id: number;
  name: string;
  phone: string | null;
  commissionPercent: number;
  tenantId: number;
}

export interface AgentPortalAuthResponse {
  token: string;
  agent: AgentPortalProfile;
}
