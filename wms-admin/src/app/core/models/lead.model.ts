export type LeadStatus = 'New' | 'Contacted' | 'DemoGiven' | 'Won' | 'Lost';
export type LeadSource = 'Website' | 'Portal' | 'Manual' | 'Referral';

export const LEAD_STATUSES: { label: string; value: LeadStatus }[] = [
  { label: 'New', value: 'New' },
  { label: 'Contacted', value: 'Contacted' },
  { label: 'Demo given', value: 'DemoGiven' },
  { label: 'Won', value: 'Won' },
  { label: 'Lost', value: 'Lost' }
];

export const LEAD_SOURCES: { label: string; value: LeadSource }[] = [
  { label: 'Website', value: 'Website' },
  { label: 'Portal', value: 'Portal' },
  { label: 'Manual', value: 'Manual' },
  { label: 'Referral', value: 'Referral' }
];

/** Status → pill klassi. `New` ko'zga tashlanadi, `Lost` so'nadi. */
export const LEAD_STATUS_CLASS: Record<LeadStatus, string> = {
  New: 'pill pill-info',
  Contacted: 'pill pill-warning',
  DemoGiven: 'pill pill-purple',
  Won: 'pill pill-success',
  Lost: 'pill pill-neutral'
};

export interface Lead {
  id: number;
  companyName: string;
  contactName: string;
  phone: string;
  email: string | null;
  note: string | null;
  source: LeadSource;
  /** Portal orqali kelgan bo'lsa — qaysi tenant orqali. */
  referrerTenantId: number | null;
  referrerTenantName: string | null;
  status: LeadStatus;
  statusNote: string | null;
  convertedTenantId: number | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface UpdateLeadDto {
  status: LeadStatus;
  statusNote: string | null;
}

/** Lead'dan tenant yaratish — tenant yaratish formasining o'zi. */
export interface ConvertLeadDto {
  name: string;
  slug: string;
  planId: number | null;
  adminFullName: string;
  adminPhone: string;
  adminPassword: string;
  paidUntil: string | null;
}
