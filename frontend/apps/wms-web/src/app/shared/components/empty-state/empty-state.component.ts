import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';

/**
 * Bo'sh ro'yxat holati (eski `empty-state`). Eski standart matn `'No data found'`
 * qotirilgan inglizcha edi — endi `message` berilmasa `common.empty` kaliti.
 */
@Component({
  selector: 'app-empty-state',
  imports: [TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex flex-col items-center justify-center py-16 text-gray-400 dark:text-gray-500"
      *transloco="let t"
    >
      <i [class]="'pi ' + icon() + ' text-5xl mb-4 opacity-50'"></i>
      <p class="text-base font-medium m-0">{{ message() ?? t('common.empty') }}</p>
    </div>
  `,
})
export class EmptyStateComponent {
  readonly icon = input<string>('pi-inbox');
  readonly message = input<string | null>(null);
}
