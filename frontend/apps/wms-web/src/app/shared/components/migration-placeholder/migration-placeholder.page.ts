import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';

import { PageHeaderComponent } from '../page-header/page-header.component';

/**
 * «Ko'chirilmoqda» — hali ko'chirilmagan ekran o'rnidagi vaqtinchalik sahifa.
 *
 * Marshrut daraxti TO'LIQ va guard'lari bilan tayyor: ekranni ko'chiruvchi
 * agent faqat `placeholder(...)` qatorini haqiqiy `loadComponent` ga
 * almashtiradi (`apps/wms-web/MIGRATION.md`). Kirishlar marshrut `data` sidan
 * keladi (`withComponentInputBinding`).
 */
@Component({
  selector: 'app-migration-placeholder',
  imports: [TranslocoDirective, PageHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-enter" *transloco="let t">
      <app-page-header [title]="t(titleKey())" [subtitle]="t('shell.migrating.title')" />
      <div class="wms-card migration-card">
        <i class="pi pi-hourglass"></i>
        <p>{{ t('shell.migrating.subtitle') }}</p>
        @if (source()) {
          <code>{{ source() }}</code>
        }
      </div>
    </div>
  `,
  styles: `
    .migration-card {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      padding: 48px 24px;
      text-align: center;
      color: var(--text-secondary);

      i {
        font-size: 32px;
        color: var(--brand-primary, var(--color-pistachio-500));
      }

      p {
        margin: 0;
        max-width: 420px;
      }

      code {
        font-family: var(--font-mono);
        font-size: 12px;
        color: var(--text-muted);
      }
    }
  `,
})
export class MigrationPlaceholderPage {
  /** Ekran nomi kaliti (menyudagi bilan bir xil). */
  readonly titleKey = input<string>('shell.migrating.title');
  /** Eski `wms-ui` dagi manba komponent (`modules/...`). */
  readonly source = input<string>('');
}
