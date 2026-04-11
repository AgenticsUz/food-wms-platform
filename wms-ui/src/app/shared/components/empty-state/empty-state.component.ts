import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center py-16 text-gray-400 dark:text-gray-500">
      <i [class]="'pi ' + icon() + ' text-5xl mb-4 opacity-50'"></i>
      <p class="text-base font-medium m-0">{{ message() }}</p>
    </div>
  `
})
export class EmptyStateComponent {
  icon = input<string>('pi-inbox');
  message = input<string>('No data found');
}
