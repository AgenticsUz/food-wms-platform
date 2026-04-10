import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-settings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div class="page-enter"><h1>Settings</h1><p style="color: var(--text-secondary)">Settings module will be implemented in Phase 4.</p></div>`
})
export default class SettingsComponent {}
