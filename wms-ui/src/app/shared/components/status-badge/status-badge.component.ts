import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span [class]="badgeClass()">{{ label() }}</span>`
})
export class StatusBadgeComponent {
  status = input.required<string>();
  label = input.required<string>();

  private statusMap: Record<string, string> = {
    Confirmed: 'inline-block bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-400 px-2.5 py-0.5 rounded-full text-xs font-semibold whitespace-nowrap',
    Completed: 'inline-block bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-400 px-2.5 py-0.5 rounded-full text-xs font-semibold whitespace-nowrap',
    Active: 'inline-block bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-400 px-2.5 py-0.5 rounded-full text-xs font-semibold whitespace-nowrap',
    Pending: 'inline-block bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-400 px-2.5 py-0.5 rounded-full text-xs font-semibold whitespace-nowrap',
    Draft: 'inline-block bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300 px-2.5 py-0.5 rounded-full text-xs font-semibold whitespace-nowrap',
    InProgress: 'inline-block bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400 px-2.5 py-0.5 rounded-full text-xs font-semibold whitespace-nowrap',
    Rejected: 'inline-block bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400 px-2.5 py-0.5 rounded-full text-xs font-semibold whitespace-nowrap',
    Cancelled: 'inline-block bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400 px-2.5 py-0.5 rounded-full text-xs font-semibold whitespace-nowrap',
  };

  private defaultClass = 'inline-block bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300 px-2.5 py-0.5 rounded-full text-xs font-semibold whitespace-nowrap';

  badgeClass = computed(() => this.statusMap[this.status()] ?? this.defaultClass);
}
