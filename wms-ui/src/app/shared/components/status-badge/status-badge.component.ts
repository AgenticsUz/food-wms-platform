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

  private base = 'inline-block px-2.5 py-0.5 rounded-full text-xs font-semibold whitespace-nowrap';

  private statusMap: Record<string, string> = {
    // Success / Confirmed / Active
    Confirmed: `${this.base} bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300`,
    Completed: `${this.base} bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300`,
    Active:    `${this.base} bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300`,

    // Warning / Pending
    Pending:   `${this.base} bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-300`,

    // Error / Rejected / Cancelled
    Rejected:  `${this.base} bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300`,
    Cancelled: `${this.base} bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300`,

    // In Progress
    InProgress: `${this.base} bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300`,

    // Draft / Neutral
    Draft:     `${this.base} bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300`,

    // Finance: Income / Expense
    Income:    `${this.base} bg-emerald-100 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300`,
    Expense:   `${this.base} bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300`,

    // Transfer types
    Incoming:  `${this.base} bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300`,
    Outgoing:  `${this.base} bg-orange-100 text-orange-800 dark:bg-orange-900/30 dark:text-orange-300`,
    Internal:  `${this.base} bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-300`,
    Production: `${this.base} bg-indigo-100 text-indigo-800 dark:bg-indigo-900/30 dark:text-indigo-300`,
  };

  private defaultClass = `${this.base} bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300`;

  badgeClass = computed(() => this.statusMap[this.status()] ?? this.defaultClass);
}
