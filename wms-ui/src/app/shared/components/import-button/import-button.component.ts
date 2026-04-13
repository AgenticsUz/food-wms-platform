import { Component, inject, input, output, signal, ChangeDetectionStrategy } from '@angular/core';
import { Dialog } from 'primeng/dialog';
import { TranslocoDirective } from '@jsverse/transloco';
import { ImportService } from '../../../core/services/import.service';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-import-button',
  standalone: true,
  imports: [Dialog, TranslocoDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './import-button.component.html',
  styleUrl: './import-button.component.scss'
})
export class ImportButtonComponent {
  private importService = inject(ImportService);
  private notify = inject(NotificationService);

  type = input.required<'products' | 'counterparties' | 'users'>();
  imported = output<void>();

  importing = signal(false);
  importResult = signal<any>(null);
  showResult = signal(false);

  downloadTemplate() {
    this.importService.downloadTemplate(this.type());
  }

  onFileSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    this.importing.set(true);
    this.importService.importFile(this.type(), file).subscribe({
      next: (result) => {
        this.importResult.set(result);
        this.showResult.set(true);
        this.importing.set(false);
        if (result.data?.successCount > 0) {
          this.notify.success(`${result.data.successCount} records imported`);
          this.imported.emit();
        }
        (event.target as HTMLInputElement).value = '';
      },
      error: () => {
        this.notify.error('Import failed');
        this.importing.set(false);
        (event.target as HTMLInputElement).value = '';
      }
    });
  }

  closeResult() {
    this.showResult.set(false);
  }
}
