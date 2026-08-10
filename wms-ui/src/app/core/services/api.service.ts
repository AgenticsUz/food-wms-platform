import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { NotificationService } from '../../shared/services/notification.service';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private notify = inject(NotificationService);
  private base = environment.apiUrl;

  get<T>(path: string, params?: Record<string, string | number | boolean>): Observable<ApiResponse<T>> {
    let httpParams = new HttpParams();
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          httpParams = httpParams.set(key, String(value));
        }
      });
    }
    return this.http.get<ApiResponse<T>>(`${this.base}/${path}`, { params: httpParams })
      .pipe(this.surfaceWarning());
  }

  post<T>(path: string, body: unknown): Observable<ApiResponse<T>> {
    return this.http.post<ApiResponse<T>>(`${this.base}/${path}`, body).pipe(this.surfaceWarning());
  }

  put<T>(path: string, body: unknown): Observable<ApiResponse<T>> {
    return this.http.put<ApiResponse<T>>(`${this.base}/${path}`, body).pipe(this.surfaceWarning());
  }

  patch<T>(path: string, body: unknown): Observable<ApiResponse<T>> {
    return this.http.patch<ApiResponse<T>>(`${this.base}/${path}`, body).pipe(this.surfaceWarning());
  }

  delete<T>(path: string): Observable<ApiResponse<T>> {
    return this.http.delete<ApiResponse<T>>(`${this.base}/${path}`).pipe(this.surfaceWarning());
  }

  /**
   * Backend muvaffaqiyatli javobga `warning` qo'shishi mumkin (limitga yaqinlashish,
   * tarifdan tashqari ruxsat). Bu **xato emas** — amaliyot bajarilgan.
   *
   * Toast shu yerda, bitta joyda chiqariladi: har komponentda takrorlansa "bitta toast"
   * qoidasi buziladi va ba'zi joylarda umuman unutiladi. Matn backenddan keladi va
   * `Accept-Language` bo'yicha allaqachon tarjima qilingan.
   */
  private surfaceWarning<T>() {
    return tap<ApiResponse<T>>(res => {
      const warning = res?.warning;
      if (warning?.message) this.notify.warn(warning.message);
    });
  }
}
