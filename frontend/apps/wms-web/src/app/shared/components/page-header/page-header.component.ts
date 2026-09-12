import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Sahifa sarlavhasi + o'ng tomonda amallar (`<ng-content>`). Eski `page-header`. */
@Component({
  selector: 'app-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex items-center justify-between mb-6 gap-4 flex-wrap">
      <div>
        <!-- Agentics \`ag-pagehead\` o'lchamlari (21px/700), ranglar — WMS tokenlari. -->
        <h1 class="m-0 text-[21px] font-bold leading-tight tracking-[-0.01em] text-[var(--text-primary)]">
          {{ title() }}
        </h1>
        @if (subtitle()) {
          <p class="m-0 mt-1 text-[13px] text-[var(--text-secondary)]">{{ subtitle() }}</p>
        }
      </div>
      <div class="flex items-center gap-3">
        <ng-content />
      </div>
    </div>
  `,
})
export class PageHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
}
