import { Injectable, inject, signal } from '@angular/core';
import { toLocalDateString } from '../../shared/utils/date.util';
import { Router } from '@angular/router';
import { ApiService } from './api.service';
import { NotificationItem } from '../models/notification.model';

const LAST_BATCH_EXPIRY_CHECK = 'lastBatchExpiryCheck';

@Injectable({ providedIn: 'root' })
export class NotificationBellService {
  private api = inject(ApiService);
  private router = inject(Router);

  notifications = signal<NotificationItem[]>([]);
  unreadCount = signal<number>(0);
  private intervalId: ReturnType<typeof setInterval> | null = null;

  loadNotifications() {
    this.api.get<NotificationItem[]>('notifications').subscribe(res => {
      if (res.success && res.data) this.notifications.set(res.data);
    });
  }

  loadUnreadCount() {
    this.api.get<{ count: number }>('notifications/unread-count').subscribe(res => {
      if (res.success && res.data) this.unreadCount.set(res.data.count);
    });
  }

  markAsRead(id: number) {
    this.api.put<void>(`notifications/${id}/read`, {}).subscribe(() => {
      this.notifications.update(list =>
        list.map(n => n.id === id ? { ...n, isRead: true } : n)
      );
      this.unreadCount.update(c => Math.max(0, c - 1));
    });
  }

  markAllAsRead() {
    this.api.put<void>('notifications/read-all', {}).subscribe(() => {
      this.notifications.update(list => list.map(n => ({ ...n, isRead: true })));
      this.unreadCount.set(0);
    });
  }

  navigateToEntity(notification: NotificationItem) {
    if (!notification.isRead) this.markAsRead(notification.id);

    if (!notification.entityType || !notification.entityId) return;

    switch (notification.entityType) {
      case 'Transfer':
        this.router.navigate(['/transfers', notification.entityId]);
        break;
      case 'Batch':
        this.router.navigate(['/warehouse/batches'], {
          queryParams: { highlight: notification.entityId }
        });
        break;
      case 'ProductionOrder':
        this.router.navigate(['/production/orders', notification.entityId]);
        break;
      case 'Product':
        this.router.navigate(['/products'], {
          queryParams: { highlight: notification.entityId }
        });
        break;
    }
  }

  /**
   * Backend tomondan batch expiry skanerini chaqiradi.
   * Idempotent — kuniga bir marta yuboramiz (localStorage bilan tekshirib).
   * Xato bo'lsa sezdirmasdan o'tib ketamiz — kritik emas.
   */
  triggerBatchExpiryCheck(warningDaysAhead = 3) {
    const today = toLocalDateString(new Date()); // YYYY-MM-DD
    const last = localStorage.getItem(LAST_BATCH_EXPIRY_CHECK);
    if (last === today) return;

    this.api
      .post<{ created: number }>(
        `notifications/check-expiring-batches?warningDaysAhead=${warningDaysAhead}`,
        null
      )
      .subscribe({
        next: () => {
          localStorage.setItem(LAST_BATCH_EXPIRY_CHECK, today);
          // yangilangan ro'yxatni darhol yuklaymiz
          this.loadUnreadCount();
          this.loadNotifications();
        },
        error: () => {
          // ignore
        }
      });
  }

  startPolling() {
    // Idempotent: qayta login/F5'da ikkinchi interval yaratilmasin
    if (this.intervalId) return;

    this.loadUnreadCount();
    this.loadNotifications();
    // Login/boot vaqtida bir marta — kuniga bitta tekshiruv
    this.triggerBatchExpiryCheck();

    this.intervalId = setInterval(() => {
      this.loadUnreadCount();
    }, 60000);
  }

  stopPolling() {
    if (this.intervalId) {
      clearInterval(this.intervalId);
      this.intervalId = null;
    }
  }
}
