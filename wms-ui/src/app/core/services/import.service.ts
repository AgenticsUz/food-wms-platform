import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { NotificationService } from '../../shared/services/notification.service';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ImportService {
  private http = inject(HttpClient);
  private notify = inject(NotificationService);
  private baseUrl = environment.apiUrl;

  downloadTemplate(type: 'products' | 'counterparties' | 'users') {
    this.http.get(`${this.baseUrl}/import/template/${type}`, {
      responseType: 'blob'
    }).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${type}-template.xlsx`;
        a.click();
        URL.revokeObjectURL(url);
        this.notify.success('Template downloaded');
      },
      error: () => this.notify.error('Download failed')
    });
  }

  importFile(type: 'products' | 'counterparties' | 'users', file: File) {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<any>(
      `${this.baseUrl}/import/${type}`,
      formData
    );
  }
}
