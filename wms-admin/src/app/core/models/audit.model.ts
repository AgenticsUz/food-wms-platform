/** `GET /api/admin/audit` — platforma bo'ylab audit yozuvi (B4). */
export interface AuditLogEntry {
  id: number;
  userId: number | null;
  userName: string | null;
  /** HTTP metod: POST / PUT / DELETE / PATCH. */
  action: string;
  /** Controller nomi — masalan `Transfers`. */
  entityType: string;
  /** Action nomi — masalan `Confirm`. */
  entityAction: string | null;
  entityId: number | null;
  path: string;
  statusCode: number;
  createdAt: string;

  /** Platforma (SuperAdmin) amali — masalan tenantni to'xtatish. */
  isPlatformAction: boolean;

  /** Amal qaysi tenantda sodir bo'lgan. */
  tenantId: number;
  tenantName: string | null;
  /** Amalni bajargan tenant (platforma amallarida — platforma tenanti). */
  actorTenantId: number | null;
  actorTenantName: string | null;
}

/**
 * Filtrlar. Barchasi ixtiyoriy: `tenantId` berilmasa **barcha** tenantlar bo'yicha.
 * Sahifalash server tomonda — audit eng tez o'sadigan jadval, uni to'liq yuklamang.
 */
export interface AuditQuery {
  tenantId?: number | null;
  entityType?: string | null;
  action?: string | null;
  platformOnly?: boolean | null;
  from?: string | null;
  to?: string | null;
  page: number;
  pageSize: number;
}

export interface PaginatedAudit {
  items: AuditLogEntry[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
