import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ApiService } from './api.service';
import { NotificationItem } from '../models/notification.model';

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
    this.markAsRead(notification.id);
    if (notification.entityType === 'Transfer' && notification.entityId) {
      this.router.navigate(['/transfers', notification.entityId]);
    }
  }

  startPolling() {
    this.loadUnreadCount();
    this.loadNotifications();
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
