import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Agentics brend belgisi geometriyasi — ochiq halqa ichidagi «A» (32×32, faqat chiziq).
 * Wash `hr-brand-mark`, HRM `hrm-brand-mark` va Identity `LoginPage.BrandMark` bilan
 * AYNAN bir xil; `public/favicon.svg` va sidebar'dagi inline nusxa ham shu yo'llar.
 */
export const BRAND_MARK_GEOMETRY = {
  ring: 'M9.75 26.8A12.5 12.5 0 1 1 26.2 23.2',
  legLight: 'M10.6 24.6 16.4 9',
  legHeavy: 'M16.4 9 23.6 27.6',
  bar: 'M12.9 19.6H24.4',
} as const;

export type BrandMarkSize = 'sm' | 'md' | 'lg';

/**
 * Gradient plitkadagi brend belgisi: `sm` 28 · `md` 40 · `lg` 56 px.
 * Uslublar global (`styles.css` → `.ag-brand-mark`), chunki sidebar ham shu
 * sinflardan foydalanadi. `ignite` — halqa chizilish animatsiyasi (kirish sahifasi).
 */
@Component({
  selector: 'app-brand-mark',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg viewBox="0 0 32 32" fill="none" stroke="currentColor" aria-hidden="true">
      <path class="ag-brand-mark__ring" [attr.d]="geometry.ring" stroke-width="2.3" />
      <path class="ag-brand-mark__leg" [attr.d]="geometry.legLight" stroke-width="1.8" />
      <path class="ag-brand-mark__leg" [attr.d]="geometry.legHeavy" stroke-width="3.4" />
      <path class="ag-brand-mark__bar" [attr.d]="geometry.bar" stroke-width="1.8" />
    </svg>
  `,
  host: {
    class: 'ag-brand-mark',
    '[class.ag-brand-mark--md]': "size() === 'md'",
    '[class.ag-brand-mark--lg]': "size() === 'lg'",
    '[class.ag-brand-mark--ignite]': 'ignite()',
    'aria-hidden': 'true',
  },
})
export class BrandMarkComponent {
  readonly size = input<BrandMarkSize>('sm');
  readonly ignite = input(false);

  protected readonly geometry = BRAND_MARK_GEOMETRY;
}
