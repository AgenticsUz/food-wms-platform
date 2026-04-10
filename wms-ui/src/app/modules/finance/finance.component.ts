import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-finance',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div class="page-enter"><h1>Finance</h1><p style="color: var(--text-secondary)">Finance module will be implemented in Phase 4.</p></div>`
})
export default class FinanceComponent {}
