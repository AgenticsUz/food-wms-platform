import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-kpi',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div class="page-enter"><h1>KPI & Shifts</h1><p style="color: var(--text-secondary)">KPI module will be implemented in Phase 4.</p></div>`
})
export default class KpiComponent {}
