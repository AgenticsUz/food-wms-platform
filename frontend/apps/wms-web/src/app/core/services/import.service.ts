import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';
import { LanguageService } from '@agentics/i18n';

import { ApiService } from '../api/api.service';
import type { ApiResponse } from '../api/api-response.model';
import { NotificationService } from '../notify/notification.service';
import { saveBlob } from '../utils/file.util';

// `users` importi F6 da o'chdi (D7): odamlar Identity/Console orqali qo'shiladi.
export type ImportType = 'products' | 'counterparties';

export interface ImportRowError {
  readonly row: number;
  readonly field: string;
  readonly message: string;
}

export interface ImportResult {
  readonly totalRows: number;
  readonly successCount: number;
  readonly errorCount: number;
  readonly errors?: readonly ImportRowError[];
}

/** Excel import (eski `import.service.ts`). API yo'llari o'zgarmagan. */
@Injectable({ providedIn: 'root' })
export class ImportService {
  private readonly api = inject(ApiService);
  private readonly notify = inject(NotificationService);
  private readonly language = inject(LanguageService);

  downloadTemplate(type: ImportType): void {
    this.api.download(`import/template/${type}`).subscribe({
      next: (blob) => {
        saveBlob(blob, `${type}-template.xlsx`);
        this.notify.success(this.language.translate('shell.export.done'));
      },
      // Xato toastini `WmsErrorNotifier` allaqachon chiqardi.
      error: () => undefined,
    });
  }

  importFile(type: ImportType, file: File): Observable<ApiResponse<ImportResult>> {
    const form = new FormData();
    form.append('file', file);
    return this.api.upload<ImportResult>(`import/${type}`, form);
  }
}
