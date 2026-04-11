import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { NotificationService } from '../../shared/services/notification.service';

@Injectable({ providedIn: 'root' })
export class ExportService {
  private http = inject(HttpClient);
  private notify = inject(NotificationService);
  private baseUrl = environment.apiUrl;

  exporting = signal(false);

  download(endpoint: string, filename: string, params?: Record<string, unknown>) {
    this.exporting.set(true);

    this.http.get(`${this.baseUrl}/${endpoint}`, {
      responseType: 'blob',
      params: this.cleanParams(params)
    }).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        a.click();
        URL.revokeObjectURL(url);
        this.notify.success('File downloaded successfully');
        this.exporting.set(false);
      },
      error: () => {
        this.notify.error('Export failed');
        this.exporting.set(false);
      }
    });
  }

  downloadPdf(transferId: number) {
    this.download(`export/transfers/${transferId}/pdf`, `transfer-${transferId}.pdf`);
  }

  private cleanParams(params?: Record<string, unknown>): Record<string, string> {
    if (!params) return {};
    return Object.fromEntries(
      Object.entries(params)
        .filter(([_, v]) => v != null && v !== '')
        .map(([k, v]) => [k, String(v)])
    );
  }
}
