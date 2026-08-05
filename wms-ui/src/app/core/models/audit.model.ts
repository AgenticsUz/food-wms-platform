export interface AuditLog {
  id: number;
  userId: number | null;
  userName: string | null;
  action: string;        // POST | PUT | DELETE | PATCH
  entityType: string;    // controller nomi
  entityAction: string | null;
  entityId: number | null;
  path: string;
  statusCode: number;
  createdAt: string;
  /** Platforma (SuperAdmin) amali — masalan tenantni suspend qilish. */
  isPlatformAction: boolean;
}
