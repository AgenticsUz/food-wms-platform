import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-products',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div class="page-enter"><h1>Products</h1><p style="color: var(--text-secondary)">Products module will be implemented in Phase 4.</p></div>`
})
export default class ProductsComponent {}
