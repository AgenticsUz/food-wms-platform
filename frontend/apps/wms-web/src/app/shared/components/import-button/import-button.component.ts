import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { Dialog } from 'primeng/dialog';
import { TranslocoDirective } from '@jsverse/transloco';
import { LanguageService } from '@agentics/i18n';

import { ImportService, type ImportResult, type ImportType } from '../../../core/services/import.service';
import { NotificationService } from '../../../core/notify/notification.service';

/** Excel shablon + import tugmalari va natija dialogi (eski `import-button`). */
@Component({
  selector: 'app-import-button',
  imports: [Dialog, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './import-button.component.html',
  styleUrl: './import-button.component.scss',
})
export class ImportButtonComponent {
  private readonly importService = inject(ImportService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  readonly type = input.required<ImportType>();
  readonly imported = output<void>();

  readonly importing = signal(false);
  readonly importResult = signal<ImportResult | null>(null);
  readonly showResult = signal(false);

  downloadTemplate(): void {
    this.importService.downloadTemplate(this.type());
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.importing.set(true);
    this.importService.importFile(this.type(), file).subscribe({
      next: (res) => {
        const result = res.data ?? null;
        this.importResult.set(result);
        this.showResult.set(result !== null);
        this.importing.set(false);
        if (result !== null && result.successCount > 0) {
          this.notify.success(this.language.translate('shell.import.done', { count: result.successCount }));
          this.imported.emit();
        }
        input.value = '';
      },
      // Xato toastini `WmsErrorNotifier` allaqachon chiqardi.
      error: () => {
        this.importing.set(false);
        input.value = '';
      },
    });
  }

  closeResult(): void {
    this.showResult.set(false);
  }
}
