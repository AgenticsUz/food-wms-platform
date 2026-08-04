import { Component, ChangeDetectionStrategy } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';

/**
 * Custom feature namunasi — yangi maxsus fitcha shu skeletdan ko'chiriladi.
 * Qoidalar: `modules/custom/README.md`.
 *
 * Ko'rinishi uchun `custom.example-feature` feature'i tenantga yoqilgan bo'lishi kerak;
 * yopiq bo'lsa menyuda ham ko'rinmaydi, to'g'ridan-to'g'ri URL ham 403 beradi.
 */
@Component({
  selector: 'app-example-feature',
  standalone: true,
  imports: [PageHeaderComponent, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page-enter" *transloco="let t">
      <app-page-header
        [title]="t('custom.exampleFeature.title')"
        [subtitle]="t('custom.exampleFeature.subtitle')" />

      <div class="wms-card">
        <p>{{ t('custom.exampleFeature.placeholder') }}</p>
      </div>
    </div>
  `
})
export default class ExampleFeatureComponent {}
