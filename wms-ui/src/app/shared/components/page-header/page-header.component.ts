import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-page-header',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex items-center justify-between mb-6 gap-4 flex-wrap">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white m-0 leading-tight">{{ title() }}</h1>
        @if (subtitle()) {
          <p class="text-sm text-gray-500 mt-0.5 m-0">{{ subtitle() }}</p>
        }
      </div>
      <div class="flex items-center gap-3">
        <ng-content />
      </div>
    </div>
  `
})
export class PageHeaderComponent {
  title = input.required<string>();
  subtitle = input<string>();
}
