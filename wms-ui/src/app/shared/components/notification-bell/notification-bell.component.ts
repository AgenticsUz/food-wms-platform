import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { NgClass } from '@angular/common';
import { Popover } from 'primeng/popover';
import { TranslocoDirective } from '@jsverse/transloco';
import { NotificationBellService } from '../../../core/services/notification-bell.service';
import { NotificationItem, NotificationType } from '../../../core/models/notification.model';
import { parseUtc } from '../../utils/date.util';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [NgClass, Popover, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [`
    .notif-item { border-bottom: 1px solid var(--border); transition: background 0.15s; }
    .notif-item:hover { background: var(--bg-muted) !important; }
    .notif-item.unread { background: rgba(94,149,64,0.06); }
    .notif-message { display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
  `],
  template: `
    <div class="relative" *transloco="let t">
      <button (click)="overlay.toggle($event)"
              class="relative p-2 rounded-lg hover:bg-gray-100
                     dark:hover:bg-gray-700 transition-all cursor-pointer
                     border-none bg-transparent">
        <i class="pi pi-bell text-lg" style="color: var(--text-secondary)"></i>
        @if (bellService.unreadCount() > 0) {
          <span class="absolute -top-0.5 -right-0.5 w-5 h-5 bg-red-500
                       text-white text-xs font-bold rounded-full
                       flex items-center justify-center"
                style="font-size: 10px; line-height: 1;">
            {{ bellService.unreadCount() > 9 ? '9+' : bellService.unreadCount() }}
          </span>
        }
      </button>

      <p-popover #overlay [style]="{ width: '380px' }">
        <!-- Header -->
        <div class="flex items-center justify-between px-4 py-3"
             style="border-bottom: 1px solid var(--border)">
          <span class="font-semibold" style="color: var(--text-primary)">
            {{ t('notifications.title') }}
          </span>
          @if (bellService.unreadCount() > 0) {
            <button (click)="bellService.markAllAsRead()"
                    class="text-xs font-medium cursor-pointer border-none bg-transparent"
                    style="color: var(--primary-500)">
              {{ t('notifications.markAllRead') }}
            </button>
          }
        </div>

        <!-- List -->
        <div class="max-h-96 overflow-y-auto">
          @if (bellService.notifications().length === 0) {
            <div class="flex flex-col items-center py-10" style="color: var(--text-muted)">
              <i class="pi pi-bell-slash text-4xl mb-2 opacity-50"></i>
              <p class="text-sm m-0">{{ t('notifications.empty') }}</p>
            </div>
          }
          @for (n of bellService.notifications(); track n.id) {
            <div (click)="onNotificationClick(n, overlay)"
                 class="notif-item flex items-start gap-3 px-4 py-3 cursor-pointer"
                 [class.unread]="!n.isRead">
              <div class="w-9 h-9 rounded-full flex items-center justify-center flex-shrink-0"
                   [ngClass]="getIconBg(n.type)">
                <i class="pi text-sm" [ngClass]="getIcon(n.type)"></i>
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-semibold m-0" style="color: var(--text-primary)">
                  {{ getTypeTitle(n.type, t) || n.title }}
                </p>
                <p class="notif-message text-xs mt-0.5 m-0" style="color: var(--text-secondary)">
                  {{ n.message }}
                </p>
                <p class="text-xs mt-1 m-0" style="color: var(--text-muted)">
                  {{ getRelativeTime(n.createdAt) }}
                </p>
              </div>
              @if (!n.isRead) {
                <div class="w-2 h-2 rounded-full flex-shrink-0 mt-1.5"
                     style="background: var(--primary-500)"></div>
              }
            </div>
          }
        </div>
      </p-popover>
    </div>
  `
})
export class NotificationBellComponent {
  bellService = inject(NotificationBellService);

  onNotificationClick(n: NotificationItem, overlay: Popover) {
    this.bellService.navigateToEntity(n);
    overlay.hide();
  }

  getIcon(type: NotificationType): string {
    const icons: Record<NotificationType, string> = {
      [NotificationType.Info]:                'pi-info-circle',
      [NotificationType.Warning]:             'pi-exclamation-triangle',
      [NotificationType.LowStock]:            'pi-box',
      [NotificationType.TransferConfirmed]:   'pi-check-circle',
      [NotificationType.TransferRejected]:    'pi-times-circle',
      [NotificationType.Error]:               'pi-times-circle',
      [NotificationType.BatchExpiring]:       'pi-clock',
      [NotificationType.BatchExpired]:        'pi-exclamation-circle',
      [NotificationType.ProductionStarted]:   'pi-cog',
      [NotificationType.ProductionCompleted]: 'pi-check-circle'
    };
    return icons[type] ?? 'pi-bell';
  }

  getIconBg(type: NotificationType): string {
    const bgs: Record<NotificationType, string> = {
      [NotificationType.Info]:                'bg-blue-100 text-blue-600 dark:bg-blue-900/20 dark:text-blue-400',
      [NotificationType.Warning]:             'bg-amber-100 text-amber-600 dark:bg-amber-900/20 dark:text-amber-400',
      [NotificationType.LowStock]:            'bg-orange-100 text-orange-600 dark:bg-orange-900/20 dark:text-orange-400',
      [NotificationType.TransferConfirmed]:   'bg-emerald-100 text-emerald-600 dark:bg-emerald-900/20 dark:text-emerald-400',
      [NotificationType.TransferRejected]:    'bg-rose-100 text-rose-600 dark:bg-rose-900/20 dark:text-rose-400',
      [NotificationType.Error]:               'bg-red-100 text-red-600 dark:bg-red-900/20 dark:text-red-400',
      [NotificationType.BatchExpiring]:       'bg-amber-100 text-amber-600 dark:bg-amber-900/20 dark:text-amber-400',
      [NotificationType.BatchExpired]:        'bg-red-100 text-red-700 dark:bg-red-900/25 dark:text-red-400',
      [NotificationType.ProductionStarted]:   'bg-indigo-100 text-indigo-600 dark:bg-indigo-900/20 dark:text-indigo-400',
      [NotificationType.ProductionCompleted]: 'bg-emerald-100 text-emerald-600 dark:bg-emerald-900/20 dark:text-emerald-400'
    };
    return bgs[type] ?? 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300';
  }

  getTypeTitle(type: NotificationType, t: (key: string) => string): string {
    const keys: Record<NotificationType, string> = {
      [NotificationType.Info]:                'notifications.types.info',
      [NotificationType.Warning]:             'notifications.types.warning',
      [NotificationType.LowStock]:            'notifications.types.lowStock',
      [NotificationType.TransferConfirmed]:   'notifications.types.transferConfirmed',
      [NotificationType.TransferRejected]:    'notifications.types.transferRejected',
      [NotificationType.Error]:               'notifications.types.error',
      [NotificationType.BatchExpiring]:       'notifications.types.batchExpiring',
      [NotificationType.BatchExpired]:        'notifications.types.batchExpired',
      [NotificationType.ProductionStarted]:   'notifications.types.productionStarted',
      [NotificationType.ProductionCompleted]: 'notifications.types.productionCompleted'
    };
    const key = keys[type];
    if (!key) return '';
    const translated = t(key);
    // Fallback: agar tarjima topilmasa transloco kalitning o'zini qaytaradi
    return translated && translated !== key ? translated : '';
  }

  getRelativeTime(dateStr: string): string {
    // Backend sanani UTC deb parse qilamiz (Z suffiksisiz kelsa ham)
    const parsed = parseUtc(dateStr);
    const diff = Date.now() - (parsed ? parsed.getTime() : Date.now());
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'Just now';
    if (mins < 60) return `${mins} min ago`;
    const hours = Math.floor(mins / 60);
    if (hours < 24) return `${hours} hour${hours > 1 ? 's' : ''} ago`;
    const days = Math.floor(hours / 24);
    return `${days} day${days > 1 ? 's' : ''} ago`;
  }
}
