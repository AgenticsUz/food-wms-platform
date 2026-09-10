import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AUTH_ROUTES } from '@agentics/auth';
import type { AppError, ErrorNotifier } from '@agentics/http';
import { LanguageService } from '@agentics/i18n';
import { TenantStore } from '@agentics/tenant';

import { WmsSession } from '../auth/wms-session';
import { NotificationService } from '../notify/notification.service';
import { isWmsApiError, LIMIT_KEYS } from './wms-api-error';

/**
 * `ERROR_NOTIFIER` ning WMS amalga oshirilishi — xato qanday KO'RSATILISHINI
 * hal qiladi (eski `wms-ui/core/interceptors/error.interceptor.ts` mantiqi).
 *
 * Nega paketdagi `ToastErrorNotifier` emas: u faqat `AppError.title` (i18n
 * kaliti) ni ko'rsatadi. WMS'da esa 400/409/422 ning MA'NOSI serverning
 * tarjima qilingan `message` ida («Omborda yetarli qoldiq yo'q») — umumiy
 * «Amalni bajarib bo'lmadi» foydalanuvchiga hech narsa aytmasdi. Obuna va
 * tarif kodlari ham WMS'niki (`trial_expired`, `module_disabled:*`), Wash'ning
 * `WSH-SUB-*` emas.
 *
 * Qoida: xato toasti FAQAT shu yerda — komponent ikkinchisini chiqarmaydi.
 * O'zi ko'rsatmoqchi bo'lgan ekran `ApiService` ga `{ skipErrorNotify: true }` beradi.
 */
@Injectable({ providedIn: 'root' })
export class WmsErrorNotifier implements ErrorNotifier {
  private readonly toast = inject(NotificationService);
  private readonly language = inject(LanguageService);
  private readonly tenants = inject(TenantStore);
  private readonly session = inject(WmsSession);
  private readonly router = inject(Router);
  private readonly routes = inject(AUTH_ROUTES);

  notify(error: AppError): void {
    const wms = isWmsApiError(error) ? error : null;
    // Server matni ustun: u allaqachon foydalanuvchi tilida.
    const serverText = wms?.message ?? error.detail ?? null;
    const t = (key: string, params?: Record<string, unknown>): string =>
      this.language.translate(key, params);

    switch (error.kind) {
      // Forma maydonlariga bog'lanadi / refresh oqimi o'zi hal qiladi — toast YO'Q.
      case 'validation':
      case 'unauthorized':
        return;

      // 402 — obuna bloki: sabab ko'rinadigan ekranga.
      case 'subscription':
        this.onBlocked(wms?.wmsCode ?? error.code ?? null, serverText);
        return;

      // 403 + `module_disabled:*` / `feature_disabled:*` — tarifda yo'q. Sahifa
      // ALMASHMAYDI: bosh sahifadagi bitta vidjet so'rovi foydalanuvchini
      // ekrandan uloqtirmasin. Guard darajasida esa `/not-in-plan` ekrani bor.
      case 'module-disabled':
        this.toast.info(t(error.title));
        return;

      // 403 kodsiz — ruxsat yo'q.
      case 'forbidden':
        this.toast.warn(t(error.title));
        return;

      // 409 — parallel tahrir: yozuvni boshqa foydalanuvchi o'zgartirgan.
      case 'conflict':
        this.toast.warn(serverText ?? t('errors.conflict'));
        return;

      case 'not-found':
        this.toast.warn(t('errors.notFound'));
        return;

      case 'server':
        this.toast.error(
          error.correlationId
            ? t('errors.serverWithId', { correlationId: error.correlationId })
            : t('errors.server')
        );
        return;

      case 'network':
        this.toast.error(t('errors.offline'));
        return;

      // 400 / 422 / 402-limit va noma'lum.
      default: {
        const code = wms?.wmsCode ?? null;
        if (code !== null && code in LIMIT_KEYS) {
          // Limit — BLOK EMAS: foydalanuvchi sahifada qoladi, faqat sababni ko'radi.
          this.toast.error(serverText ? `${t(LIMIT_KEYS[code])} — ${serverText}` : t(LIMIT_KEYS[code]));
          return;
        }
        this.toast.error(serverText ?? t(error.title));
      }
    }
  }

  /**
   * Obuna to'xtatildi (so'rovlar orasida). `TenantStore.markBlocked` —
   * `tenantGuard` endi qobiqqa qo'ymaydi; ekran `publicMessage ?? message` ni
   * `TenantStore.blockMessage` dan o'qiydi.
   */
  private onBlocked(code: string | null, serverText: string | null): void {
    this.session.markBlocked(code);
    this.tenants.markBlocked(serverText);
    if (!this.router.url.startsWith(this.routes.subscription)) {
      void this.router.navigateByUrl(this.routes.subscription);
    }
  }
}
