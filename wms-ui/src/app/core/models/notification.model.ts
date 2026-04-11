export interface NotificationItem {
  id: number;
  title: string;
  message: string;
  type: number; // 1=Info, 2=Warning, 3=LowStock, 4=TransferConfirmed, 5=TransferRejected, 6=Error
  entityType?: string;
  entityId?: number;
  isRead: boolean;
  readAt?: string;
  createdAt: string;
}
