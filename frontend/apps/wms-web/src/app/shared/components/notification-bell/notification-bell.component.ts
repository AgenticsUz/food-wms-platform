import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NgClass } from '@angular/common';
import { Popover } from 'primeng/popover';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';

import { NotificationBellService } from '../../../core/services/notification-bell.service';
import { NotificationType, type NotificationItem } from '../../../core/models/notification.model';
import { parseUtc } from '../../../core/utils/date.util';

const ICONS: Readonly<Record<NotificationType, string>> = {
  [NotificationType.Info]: 'pi-info-circle',
  [NotificationType.Warning]: 'pi-exclamation-triangle',
  [NotificationType.LowStock]: 'pi-box',
  [NotificationType.TransferConfirmed]: 'pi-check-circle',
  [NotificationType.TransferRejected]: 'pi-times-circle',
  [NotificationType.Error]: 'pi-times-circle',
  [NotificationType.BatchExpiring]: 'pi-clock',
  [NotificationType.BatchExpired]: 'pi-exclamation-circle',
  [NotificationType.ProductionStarted]: 'pi-cog',
  [NotificationType.ProductionCompleted]: 'pi-check-circle',
};

const ICON_BG: Readonly<Record<NotificationType, string>> = {
  [NotificationType.Info]: 'bg-blue-100 text-blue-600 dark:bg-blue-900/20 dark:text-blue-400',
  [NotificationType.Warning]: 'bg-amber-100 text-amber-600 dark:bg-amber-900/20 dark:text-amber-400',
  [NotificationType.LowStock]: 'bg-orange-100 text-orange-600 dark:bg-orange-900/20 dark:text-orange-400',
  [NotificationType.TransferConfirmed]:
    'bg-emerald-100 text-emerald-600 dark:bg-emerald-900/20 dark:text-emerald-400',
  [NotificationType.TransferRejected]: 'bg-rose-100 text-rose-600 dark:bg-rose-900/20 dark:text-rose-400',
  [NotificationType.Error]: 'bg-red-100 text-red-600 dark:bg-red-900/20 dark:text-red-400',
  [NotificationType.BatchExpiring]: 'bg-amber-100 text-amber-600 dark:bg-amber-900/20 dark:text-amber-400',
  [NotificationType.BatchExpired]: 'bg-red-100 text-red-700 dark:bg-red-900/25 dark:text-red-400',
  [NotificationType.ProductionStarted]:
    'bg-indigo-100 text-indigo-600 dark:bg-indigo-900/20 dark:text-indigo-400',
  [NotificationType.ProductionCompleted]:
    'bg-emerald-100 text-emerald-600 dark:bg-emerald-900/20 dark:text-emerald-400',
};

const TYPE_KEYS: Readonly<Record<NotificationType, string>> = {
  [NotificationType.Info]: 'notifications.types.info',
  [NotificationType.Warning]: 'notifications.types.warning',
  [NotificationType.LowStock]: 'notifications.types.lowStock',
  [NotificationType.TransferConfirmed]: 'notifications.types.transferConfirmed',
  [NotificationType.TransferRejected]: 'notifications.types.transferRejected',
  [NotificationType.Error]: 'notifications.types.error',
  [NotificationType.BatchExpiring]: 'notifications.types.batchExpiring',
  [NotificationType.BatchExpired]: 'notifications.types.batchExpired',
  [NotificationType.ProductionStarted]: 'notifications.types.productionStarted',
  [NotificationType.ProductionCompleted]: 'notifications.types.productionCompleted',
};

/**
 * Topbar qo'ng'irog'i (eski `notification-bell`). Nisbiy vaqt endi qotirilgan
 * inglizcha («5 min ago») emas — `Intl.RelativeTimeFormat` faol til lokalida.
 */
@Component({
  selector: 'app-notification-bell',
  imports: [NgClass, Popover, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './notification-bell.component.html',
  styleUrl: './notification-bell.component.scss',
})
export class NotificationBellComponent {
  readonly bell = inject(NotificationBellService);
  private readonly language = inject(LanguageService);

  onNotificationClick(n: NotificationItem, overlay: Popover): void {
    this.bell.navigateToEntity(n);
    overlay.hide();
  }

  icon(type: NotificationType): string {
    return ICONS[type] ?? 'pi-bell';
  }

  iconBg(type: NotificationType): string {
    return ICON_BG[type] ?? 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300';
  }

  /** Tur nomi; kalit bo'lmasa serverdagi sarlavha. */
  title(n: NotificationItem): string {
    const key = TYPE_KEYS[n.type];
    if (!key) return n.title;
    const translated = this.language.translate(key);
    return translated && translated !== key ? translated : n.title;
  }

  relativeTime(dateStr: string): string {
    const parsed = parseUtc(dateStr);
    const diffMinutes = Math.round(((parsed?.getTime() ?? Date.now()) - Date.now()) / 60_000);
    const format = new Intl.RelativeTimeFormat(this.language.locale(), { numeric: 'auto' });
    if (Math.abs(diffMinutes) < 60) return format.format(diffMinutes, 'minute');
    const diffHours = Math.round(diffMinutes / 60);
    if (Math.abs(diffHours) < 24) return format.format(diffHours, 'hour');
    return format.format(Math.round(diffHours / 24), 'day');
  }
}
