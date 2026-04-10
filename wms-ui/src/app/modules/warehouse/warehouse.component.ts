import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-warehouse',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-enter">
      <h1>Warehouse</h1>
      <p style="color: var(--text-secondary)">Warehouse module will be implemented in Phase 4.</p>
    </div>
  `
})
export default class WarehouseComponent {}
