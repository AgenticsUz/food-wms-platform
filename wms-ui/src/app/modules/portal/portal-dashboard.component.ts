import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-portal-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-enter">
      <h1>Portal Dashboard</h1>
      <p style="color: var(--text-secondary)">Portal dashboard will be implemented in Phase 4.</p>
    </div>
  `
})
export default class PortalDashboardComponent {}
