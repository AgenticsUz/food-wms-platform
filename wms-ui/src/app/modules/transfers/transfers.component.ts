import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-transfers',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div class="page-enter"><h1>Transfers</h1><p style="color: var(--text-secondary)">Transfers module will be implemented in Phase 4.</p></div>`
})
export default class TransfersComponent {}
