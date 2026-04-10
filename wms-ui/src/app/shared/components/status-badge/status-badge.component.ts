import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span [class]="badgeClass()">{{ label() }}</span>`,
  styles: [`
    span {
      display: inline-block;
      padding: 4px 12px;
      border-radius: 20px;
      font-size: 12px;
      font-weight: 600;
      white-space: nowrap;
    }

    .badge-success { background: #d1fae5; color: #065f46; }
    .badge-warning { background: #fef3c7; color: #92400e; }
    .badge-danger  { background: #fee2e2; color: #991b1b; }
    .badge-info    { background: #dbeafe; color: #1e40af; }
    .badge-neutral { background: #f1f5f9; color: #475569; }
  `]
})
export class StatusBadgeComponent {
  status = input.required<string>();
  label = input.required<string>();

  private statusMap: Record<string, string> = {
    Confirmed: 'badge-success',
    Completed: 'badge-success',
    Active: 'badge-success',
    Pending: 'badge-warning',
    Draft: 'badge-warning',
    InProgress: 'badge-info',
    Rejected: 'badge-danger',
    Cancelled: 'badge-danger',
  };

  badgeClass = computed(() => this.statusMap[this.status()] ?? 'badge-neutral');
}
