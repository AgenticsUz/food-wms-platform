import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { tap, type MonoTypeOperatorFunction, type Observable } from 'rxjs';
import { httpFlags } from '@agentics/http';

import { NotificationService } from '../notify/notification.service';
import type { ApiResponse, ApiWarning } from './api-response.model';

/** So'rov parametri qiymati. `Date` UTC ISO satriga aylanadi (`date.util.ts` qoidasi). */
export type QueryValue = string | number | boolean | Date | null | undefined;
export type QueryParams = Readonly<Record<string, QueryValue | readonly QueryValue[]>>;

/** So'rov bayroqlari — interceptor zanjiriga uzatiladi. */
export interface ApiCallOptions {
  /** Xato toasti chiqmasin — ekran xatoni o'zi ko'rsatadi. `WmsApiError` baribir tashlanadi. */
  readonly skipErrorNotify?: boolean;
  /** Global progress chizig'i ko'rsatilmasin (fon so'rovi, polling). */
  readonly skipLoading?: boolean;
}

/**
 * WMS API'ga chiqishning YAGONA nuqtasi — eski `wms-ui` `ApiService` i bilan
 * bir xil imzo: `get<T>(path, params)` → `Observable<ApiResponse<T>>`.
 *
 * ⚠️ NEGA `@agentics/http` ning `ApiClient` i EMAS. `ApiClient` `{ data, meta,
 * errors }` konvertini ochib, chaqiruvchiga faqat `data` ni beradi. WMS'da esa:
 *  - konvert boshqa (`success`, `message`, `code`, `warning` — D15) va
 *    `warning` MUVAFFAQIYATLI javobda keladi — `data` ni ochib tashlasak u yo'qoladi;
 *  - 60+ ko'chiriladigan ekran `res.success && res.data` naqshida yozilgan —
 *    imzo saqlansa servislar va komponentlar o'zgarishsiz ko'chadi.
 *
 * Paketdan olinadiganlar esa o'z joyida: token, 401 da refresh, `Accept-Language`,
 * `apiUrl` prefiksi, progress, retry — hammasi interceptor zanjirida
 * (`provideWmsHttp`). Xato `WmsApiError` bo'lib keladi va toast allaqachon
 * chiqarilgan.
 *
 * YO'L — `apiUrl` ga NISBIY, `/api` siz: `api.get('warehouses')` → `/api/warehouses`.
 * Prefiksni paketning `apiUrlInterceptor` i qo'yadi.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly notify = inject(NotificationService);

  private readonly warningState = signal<ApiWarning | null>(null);

  /**
   * Oxirgi `warning`. Obuna/limit ekrani (`limit-notice`) shunga `effect` bilan
   * qarab `subscription/me` ni qayta o'qiydi: ombor yaratilgach hisob eskirib
   * qolmasin (eski `refreshLimits` o'rniga — aylanma bog'liqliksiz).
   */
  readonly lastWarning = this.warningState.asReadonly();

  get<T>(path: string, params?: QueryParams, options?: ApiCallOptions): Observable<ApiResponse<T>> {
    return this.http
      .get<ApiResponse<T>>(path, { params: toHttpParams(params), context: toContext(options) })
      .pipe(this.surfaceWarning());
  }

  post<T>(path: string, body: unknown, options?: ApiCallOptions): Observable<ApiResponse<T>> {
    return this.http
      .post<ApiResponse<T>>(path, body, { context: toContext(options) })
      .pipe(this.surfaceWarning());
  }

  put<T>(path: string, body: unknown, options?: ApiCallOptions): Observable<ApiResponse<T>> {
    return this.http
      .put<ApiResponse<T>>(path, body, { context: toContext(options) })
      .pipe(this.surfaceWarning());
  }

  patch<T>(path: string, body: unknown, options?: ApiCallOptions): Observable<ApiResponse<T>> {
    return this.http
      .patch<ApiResponse<T>>(path, body, { context: toContext(options) })
      .pipe(this.surfaceWarning());
  }

  delete<T>(path: string, options?: ApiCallOptions): Observable<ApiResponse<T>> {
    return this.http
      .delete<ApiResponse<T>>(path, { context: toContext(options) })
      .pipe(this.surfaceWarning());
  }

  /** Fayl yuklash (`multipart/form-data`) — Excel import, logo. */
  upload<T>(path: string, form: FormData, options?: ApiCallOptions): Observable<ApiResponse<T>> {
    return this.post<T>(path, form, options);
  }

  /** Fayl olish (Excel/PDF eksport, shablon). Saqlash — `saveBlob()` (`core/utils/file.util.ts`). */
  download(path: string, params?: QueryParams, options?: ApiCallOptions): Observable<Blob> {
    return this.http.get(path, {
      params: toHttpParams(params),
      context: toContext(options),
      responseType: 'blob',
    });
  }

  /**
   * Muvaffaqiyatli javobdagi `warning` — XATO EMAS, amal bajarilgan. Toast shu
   * yerda, bitta joyda: har komponentda takrorlansa ba'zi joylarda unutiladi.
   * Matn serverdan, allaqachon tarjima qilingan.
   */
  private surfaceWarning<T>(): MonoTypeOperatorFunction<ApiResponse<T>> {
    return tap((res) => {
      const warning = res?.warning;
      if (!warning?.message) {
        return;
      }
      this.notify.warn(warning.message);
      this.warningState.set(warning);
    });
  }
}

function toHttpParams(params: QueryParams | undefined): HttpParams {
  let result = new HttpParams();
  if (params === undefined) {
    return result;
  }
  for (const [key, raw] of Object.entries(params)) {
    const values = Array.isArray(raw) ? raw : [raw];
    for (const value of values as readonly QueryValue[]) {
      if (value === null || value === undefined) {
        continue;
      }
      result = result.append(key, value instanceof Date ? value.toISOString() : String(value));
    }
  }
  return result;
}

function toContext(options: ApiCallOptions | undefined): HttpContext | undefined {
  if (options === undefined) {
    return undefined;
  }
  return httpFlags({
    skipErrorNotify: options.skipErrorNotify,
    skipLoading: options.skipLoading,
  });
}
