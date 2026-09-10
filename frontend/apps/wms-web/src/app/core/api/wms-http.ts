import {
  HttpErrorResponse,
  HttpResponse,
  provideHttpClient,
  withFetch,
  withInterceptors,
  type HttpInterceptorFn,
} from '@angular/common/http';
import { inject, makeEnvironmentProviders, type EnvironmentProviders } from '@angular/core';
import { catchError, map, throwError } from 'rxjs';
import { ConfigService } from '@agentics/config';
import {
  apiUrlInterceptor,
  authInterceptor,
  correlationInterceptor,
  CorrelationService,
  ERROR_NOTIFIER,
  loadingInterceptor,
  localeInterceptor,
  retryInterceptor,
  shouldNotify,
  SKIP_ERROR_NOTIFY,
  tenantInterceptor,
  toAppError,
} from '@agentics/http';

import { toPlatformUser } from '../auth/me-adapter';
import type { WmsMe } from '../auth/wms-me.model';
import { isApiResponse } from './api-response.model';
import { isMeUrl, isWmsApiUrl } from './api-url';
import { WmsErrorNotifier } from './wms-error-notifier';
import { toWmsApiError } from './wms-api-error';

/**
 * 7-interceptor (paketdagi `errorInterceptor` O'RNIDA): xato → `WmsApiError`.
 *
 * WMS manzilidan kelgan xato WMS konverti bo'yicha o'qiladi (`message`, `code`),
 * qolgani (Identity sessiya endpointlari) — platformaning `toAppError` i bilan.
 * Ikkala holatda ham natija `AppError` shartnomasiga mos: `authInterceptor`
 * (tashqi halqa) 401 ni `status` bo'yicha taniydi va refresh qiladi.
 */
export const wmsErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifier = inject(ERROR_NOTIFIER);
  const correlation = inject(CorrelationService);
  const config = inject(ConfigService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }
      const correlationId = correlation.last() ?? undefined;
      const appError = isWmsApiUrl(error.url ?? req.url, config.apiUrl())
        ? toWmsApiError(error, correlationId)
        : toAppError(error, correlationId);

      if (!req.context.get(SKIP_ERROR_NOTIFY) && shouldNotify(appError)) {
        notifier.notify(appError);
      }
      return throwError(() => appError);
    })
  );
};

/**
 * 9-interceptor (eng ichki): `GET {apiUrl}/me` javobini platforma shakliga moslaydi.
 *
 * `AuthService.loadCurrentUser()` `ApiClient.get('me')` ni chaqiradi; u konvertning
 * `data` sini ochadi (`'data' in body` — WMS konvertiga ham mos keladi) va
 * natijani `CurrentUserStore` / `TenantStore` ga uzatadi. Moslash sababi —
 * `me-adapter.ts` izohida. Boshqa hech bir so'rovga tegilmaydi.
 */
export const wmsMeInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.method !== 'GET' || !isMeUrl(req.url, inject(ConfigService).apiUrl())) {
    return next(req);
  }
  return next(req).pipe(
    map((event) => {
      if (!(event instanceof HttpResponse) || !isApiResponse(event.body)) {
        return event;
      }
      const body = event.body;
      if (!body.success || body.data === null || body.data === undefined) {
        return event;
      }
      return event.clone({ body: { ...body, data: toPlatformUser(body.data as WmsMe) } });
    })
  );
};

/**
 * Interceptor zanjiri — `@agentics/http` ning `CORE_HTTP_INTERCEPTORS` tartibi,
 * IKKI farq bilan:
 *
 *  - 7-o'rinda `errorInterceptor` o'rniga `wmsErrorInterceptor` (WMS konverti);
 *  - oxirida `wmsMeInterceptor` (`/me` moslagichi).
 *
 * Qolgan halqalar — correlation, tenant, auth (Bearer + 401 da single-flight
 * refresh), locale (`Accept-Language`), apiUrl, loading, retry (faqat GET) —
 * paketniki va o'zgarishsiz. `retryInterceptor` `wmsErrorInterceptor` dan ICHKARIDA
 * qoladi: u `HttpErrorResponse` ni kutadi (502/503/504), `AppError` ni emas.
 */
export const WMS_HTTP_INTERCEPTORS: readonly HttpInterceptorFn[] = [
  correlationInterceptor,
  tenantInterceptor,
  authInterceptor,
  localeInterceptor,
  apiUrlInterceptor,
  loadingInterceptor,
  wmsErrorInterceptor,
  retryInterceptor,
  wmsMeInterceptor,
];

/**
 * HTTP qatlami — `provideCoreHttp()` O'RNIGA.
 *
 * ⚠️ Nega paketdagi `provideCoreHttp()` emas: u `errorInterceptor` ni zanjirga
 * qotirib qo'yadi, u esa RFC 9457 ni kutadi. WMS `/api` xatosi esa eski
 * `ApiResponse` konvertida (D15) — paket interceptori `message` ni ham, `code`
 * ni ham ko'rmasdi va har 402 ni «noma'lum» deb o'qirdi.
 */
export function provideWmsHttp(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideHttpClient(withFetch(), withInterceptors([...WMS_HTTP_INTERCEPTORS])),
    { provide: ERROR_NOTIFIER, useExisting: WmsErrorNotifier },
  ]);
}
