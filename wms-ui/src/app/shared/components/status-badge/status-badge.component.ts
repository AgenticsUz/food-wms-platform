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
    // Success / Confirmed / Active
    Confirmed: 'pill pill-success',
    Completed: 'pill pill-success',
    Active:    'pill pill-success',

    // Warning / Pending / Draft
    Pending:   'pill pill-warning',
    Draft:     'pill pill-warning',

    // Danger / Rejected / Cancelled
    Rejected:  'pill pill-danger',
    Cancelled: 'pill pill-danger',

    // Info / In progress
    InProgress: 'pill pill-info',

    // Finance: Income / Expense
    Income:    'pill pill-success',
    Expense:   'pill pill-danger',

    // Transfer types
    Incoming:   'pill pill-info',
    Outgoing:   'pill pill-warning',
    Internal:   'pill pill-neutral',
    Production: 'pill pill-info',
    Return:     'pill pill-danger',
  };

  private defaultClass = 'pill pill-neutral';

  badgeClass = computed(() => this.statusMap[this.status()] ?? this.defaultClass);
}
