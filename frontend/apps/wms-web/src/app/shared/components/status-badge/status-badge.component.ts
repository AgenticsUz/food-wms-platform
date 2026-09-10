import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Holat belgisi (eski `status-badge`): `status` — CSS kaliti, `label` — tayyor matn. */
@Component({
  selector: 'app-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span [class]="badgeClass()">{{ label() }}</span>`,
})
export class StatusBadgeComponent {
  readonly status = input.required<string>();
  readonly label = input.required<string>();

  private readonly statusMap: Readonly<Record<string, string>> = {
    Confirmed: 'pill pill-success',
    Completed: 'pill pill-success',
    Active: 'pill pill-success',
    Pending: 'pill pill-warning',
    Draft: 'pill pill-warning',
    Rejected: 'pill pill-danger',
    Cancelled: 'pill pill-danger',
    InProgress: 'pill pill-info',
    Income: 'pill pill-success',
    Expense: 'pill pill-danger',
    Incoming: 'pill pill-info',
    Outgoing: 'pill pill-warning',
    Internal: 'pill pill-neutral',
    Production: 'pill pill-info',
    Return: 'pill pill-danger',
  };

  readonly badgeClass = computed(() => this.statusMap[this.status()] ?? 'pill pill-neutral');
}
