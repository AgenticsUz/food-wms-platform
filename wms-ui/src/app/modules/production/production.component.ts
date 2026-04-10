import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-production',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div class="page-enter"><h1>Production</h1><p style="color: var(--text-secondary)">Production module will be implemented in Phase 4.</p></div>`
})
export default class ProductionComponent {}
