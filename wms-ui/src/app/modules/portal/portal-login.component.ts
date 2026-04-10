import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-portal-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-enter" style="display: flex; align-items: center; justify-content: center; min-height: 100vh; background: var(--bg-base);">
      <div class="wms-card" style="max-width: 400px; width: 100%; text-align: center;">
        <h2>Portal Login</h2>
        <p style="color: var(--text-secondary)">Counterparty portal login will be implemented in Phase 4.</p>
      </div>
    </div>
  `
})
export default class PortalLoginComponent {}
