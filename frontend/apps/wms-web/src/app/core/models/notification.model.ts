/** Bildirishnoma turi — backend `NotificationType` bilan bir xil raqamlar. */
export enum NotificationType {
  Info = 1,
  Warning = 2,
  LowStock = 3,
  TransferConfirmed = 4,
  TransferRejected = 5,
  Error = 6,
  BatchExpiring = 7,
  BatchExpired = 8,
  ProductionStarted = 9,
  ProductionCompleted = 10,
  ReturnReceived = 11,
  /** Sinov/to'lov muddati tugayapti (TG5). */
  SubscriptionWarning = 12,
  SubscriptionSuspended = 13,
  /** Tarif limitiga yaqin (TG5). */
  LimitWarning = 14,
  /** Tasdiq kutayotgan transfer — Telegram'da tugmalar (TG9). */
  TransferPending = 15,
  ProductionPending = 16,
}

/** Bitta bildirishnoma. Id'lar F6 dan beri Guid satr (D3). */
export interface NotificationItem {
  readonly id: string;
  readonly title: string;
  readonly message: string;
  readonly type: NotificationType;
  readonly entityType?: string | null;
  readonly entityId?: string | null;
  readonly isRead: boolean;
  readonly readAt?: string | null;
  readonly createdAt: string;
}
