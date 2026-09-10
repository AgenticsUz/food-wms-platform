import { Injectable, inject, signal } from '@angular/core';
import { LanguageService } from '@agentics/i18n';

import { ApiService, type QueryParams } from '../api/api.service';
import { NotificationService } from '../notify/notification.service';
import { saveBlob } from '../utils/file.util';

/** Excel/PDF eksport (eski `export.service.ts`). API yo'llari o'zgarmagan. */
@Injectable({ providedIn: 'root' })
export class ExportService {
  private readonly api = inject(ApiService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  readonly exporting = signal(false);

  /** Bo'sh parametrlar yuborilmaydi (`''` — filtr tanlanmagan). */
  download(endpoint: string, filename: string, params?: QueryParams): void {
    this.exporting.set(true);
    const clean = params
      ? Object.fromEntries(Object.entries(params).filter(([, v]) => v !== null && v !== undefined && v !== ''))
      : undefined;

    this.api.download(endpoint, clean).subscribe({
      next: (blob) => {
        saveBlob(blob, filename);
        this.notify.success(this.language.translate('shell.export.done'));
        this.exporting.set(false);
      },
      // Xato toastini `WmsErrorNotifier` allaqachon chiqardi.
      error: () => this.exporting.set(false),
    });
  }

  downloadPdf(transferId: string): void {
    this.download(`export/transfers/${transferId}/pdf`, `transfer-${transferId}.pdf`);
  }
}
