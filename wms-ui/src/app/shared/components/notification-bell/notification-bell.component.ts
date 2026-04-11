import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { NgClass } from '@angular/common';
import { Popover } from 'primeng/popover';
import { TranslocoDirective } from '@jsverse/transloco';
import { NotificationBellService } from '../../../core/services/notification-bell.service';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [NgClass, Popover, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [`
    .notif-item { border-bottom: 1px solid var(--border); transition: background 0.15s; }
    .notif-item:hover { background: var(--bg-muted) !important; }
    .notif-item.unread { background: rgba(99,102,241,0.05); }
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
                  {{ n.title }}
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

  onNotificationClick(n: { id: number; entityType?: string; entityId?: number }, overlay: Popover) {
    this.bellService.navigateToEntity(n as any);
    overlay.hide();
  }

  getIcon(type: number): string {
    const icons: Record<number, string> = {
      1: 'pi-info-circle', 2: 'pi-exclamation-triangle', 3: 'pi-exclamation-triangle',
      4: 'pi-check-circle', 5: 'pi-times-circle', 6: 'pi-times-circle'
    };
    return icons[type] ?? 'pi-bell';
  }

  getIconBg(type: number): string {
    const bgs: Record<number, string> = {
      1: 'bg-blue-100 text-blue-600', 2: 'bg-amber-100 text-amber-600',
      3: 'bg-amber-100 text-amber-600', 4: 'bg-emerald-100 text-emerald-600',
      5: 'bg-red-100 text-red-600', 6: 'bg-red-100 text-red-600'
    };
    return bgs[type] ?? 'bg-gray-100 text-gray-600';
  }

  getRelativeTime(dateStr: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'Just now';
    if (mins < 60) return `${mins} min ago`;
    const hours = Math.floor(mins / 60);
    if (hours < 24) return `${hours} hour${hours > 1 ? 's' : ''} ago`;
    const days = Math.floor(hours / 24);
    return `${days} day${days > 1 ? 's' : ''} ago`;
  }
}
