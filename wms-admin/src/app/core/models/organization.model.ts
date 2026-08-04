/**
 * Platforma darajasidagi kompaniya. Bir xil STIR li counterparty yozuvlari va
 * tenantlar shu bitta yozuvga bog'lanadi — "kim kim bilan ishlaydi" ko'rinishi
 * shundan chiqadi.
 */
export interface Organization {
  id: number;
  name: string;
  inn: string | null;
  phone: string | null;
  address: string | null;
  tenantCount: number;
  counterpartyCount: number;
  createdAt: string;
}

export interface OrganizationLink {
  tenantId: number;
  tenantName: string;
  /** Shu tenantda counterparty sifatida qanday nom bilan turibdi. */
  counterpartyName?: string | null;
}

export interface OrganizationDetail extends Organization {
  /** Shu Organization o'z tizimiga ega bo'lgan tenantlar. */
  tenants: { id: number; name: string; slug: string }[];
  /** Qaysi tenantlarda counterparty sifatida uchraydi. */
  counterparties: OrganizationLink[];
}
