import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { ApiService, type ApiCallOptions } from '../api/api.service';
import type { NotificationItem } from '../models/notification.model';
import { toLocalDateString } from '../utils/date.util';

const LAST_BATCH_EXPIRY_CHECK = 'wms.lastBatchExpiryCheck';

/**
 * Fon so'rovlari JIM: polling har daqiqa ketadi va vaqtinchalik xato har
 * daqiqada toast bo'lib chiqmasligi kerak (ko'chish davrida endpoint hali
 * bo'lmasa ham). Progress chizig'i ham miltillamaydi.
 */
const BACKGROUND: ApiCallOptions = { skipErrorNotify: true, skipLoading: true };

/**
 * Topbar qo'ng'irog'i (eski `notification-bell.service.ts`). API yo'llari
 * o'zgarmagan (`notifications`, `notifications/unread-count`, …).
 */
@Injectable({ providedIn: 'root' })
export class NotificationBellService {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  readonly notifications = signal<readonly NotificationItem[]>([]);
  readonly unreadCount = signal<number>(0);
  private intervalId: ReturnType<typeof setInterval> | null = null;

  loadNotifications(): void {
    this.api.get<NotificationItem[]>('notifications', undefined, BACKGROUND).subscribe({
      next: (res) => {
        if (res.success && res.data) this.notifications.set(res.data);
      },
      error: () => undefined,
    });
  }

  loadUnreadCount(): void {
    this.api.get<{ count: number }>('notifications/unread-count', undefined, BACKGROUND).subscribe({
      next: (res) => {
        if (res.success && res.data) this.unreadCount.set(res.data.count);
      },
      error: () => undefined,
    });
  }

  markAsRead(id: string): void {
    this.api.put<void>(`notifications/${id}/read`, {}).subscribe(() => {
      this.notifications.update((list) => list.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
      this.unreadCount.update((c) => Math.max(0, c - 1));
    });
  }

  markAllAsRead(): void {
    this.api.put<void>('notifications/read-all', {}).subscribe(() => {
      this.notifications.update((list) => list.map((n) => ({ ...n, isRead: true })));
      this.unreadCount.set(0);
    });
  }

  navigateToEntity(notification: NotificationItem): void {
    if (!notification.isRead) this.markAsRead(notification.id);
    if (!notification.entityType || !notification.entityId) return;

    switch (notification.entityType) {
      case 'Transfer':
        void this.router.navigate(['/transfers', notification.entityId]);
        break;
      case 'Batch':
        void this.router.navigate(['/warehouse/batches'], {
          queryParams: { highlight: notification.entityId },
        });
        break;
      case 'ProductionOrder':
        void this.router.navigate(['/production/orders', notification.entityId]);
        break;
      case 'Product':
        void this.router.navigate(['/products'], {
          queryParams: { highlight: notification.entityId },
        });
        break;
    }
  }

  /**
   * Partiya muddati skanerini chaqiradi — kuniga bir marta (idempotent backend).
   * Xato bo'lsa sezdirmasdan o'tadi: kritik emas.
   */
  triggerBatchExpiryCheck(warningDaysAhead = 3): void {
    const today = toLocalDateString(new Date());
    if (readStorage(LAST_BATCH_EXPIRY_CHECK) === today) return;

    this.api
      .post<{ created: number }>(
        `notifications/check-expiring-batches?warningDaysAhead=${warningDaysAhead}`,
        null,
        BACKGROUND
      )
      .subscribe({
        next: () => {
          writeStorage(LAST_BATCH_EXPIRY_CHECK, today);
          this.loadUnreadCount();
          this.loadNotifications();
        },
        error: () => undefined,
      });
  }

  /** Idempotent: qobiq qayta yaratilsa ikkinchi interval ochilmaydi. */
  startPolling(): void {
    if (this.intervalId) return;
    this.loadUnreadCount();
    this.loadNotifications();
    this.triggerBatchExpiryCheck();
    this.intervalId = setInterval(() => this.loadUnreadCount(), 60_000);
  }

  stopPolling(): void {
    if (this.intervalId) {
      clearInterval(this.intervalId);
      this.intervalId = null;
    }
  }
}

function readStorage(key: string): string | null {
  try {
    return globalThis.localStorage?.getItem(key) ?? null;
  } catch {
    return null;
  }
}

function writeStorage(key: string, value: string): void {
  try {
    globalThis.localStorage?.setItem(key, value);
  } catch {
    // Maxfiylik rejimi — tekshiruv keyingi ochilishda takrorlanadi, xolos.
  }
}
