import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-enter">
      <h1>Dashboard</h1>
      <p style="color: var(--text-secondary)">Dashboard content will be implemented in Phase 4.</p>
    </div>
  `
})
export default class DashboardComponent {}
