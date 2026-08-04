import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../../shared/services/notification.service';
import { blockedReasonKey, isBlockingCode, LIMIT_KEYS } from '../models/subscription.model';

const SUBSCRIPTION_PAGE = '/settings/subscription';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notify = inject(NotificationService);
  const router = inject(Router);
  const transloco = inject(TranslocoService);

  // Login so'rovlarida xatoni login komponenti o'zi ko'rsatadi — bu yerda takrorlamaymiz
  const isLoginRequest = req.url.includes('/login');

  return next(req).pipe(
    catchError(err => {
      // Don't show notification for 401 (handled by auth interceptor)
      if (err.status === 401 || isLoginRequest) {
        return throwError(() => err);
      }

      const message = err.error?.message || err.message || 'An unexpected error occurred';
      const code: string = err.error?.code ?? '';

      switch (err.status) {
        case 400:
          notify.error(message);
          break;
        case 402: {
          if (isBlockingCode(code)) {
            // Backend o'z matnini yuborgan bo'lsa — o'sha ustun turadi
            notify.error(err.error?.blockedMessage || transloco.translate(blockedReasonKey(code)));
            // Obuna sahifasi enforcement'dan ozod, ya'ni sabab ko'rinadigan joy
            if (!router.url.startsWith(SUBSCRIPTION_PAGE)) {
              router.navigate([SUBSCRIPTION_PAGE]);
            }
            break;
          }
          const limitKey = LIMIT_KEYS[code];
          // Limit oshgan — foydalanuvchi shu sahifada qoladi, faqat sababni ko'radi
          notify.error(limitKey ? `${transloco.translate(limitKey)} — ${message}` : message);
          break;
        }
        case 403:
          if (code.startsWith('module_disabled')) {
            notify.error(transloco.translate('errors.moduleDisabled'));
          } else {
            notify.error(transloco.translate('errors.accessDenied'));
          }
          break;
        case 404:
          notify.error(transloco.translate('errors.notFound'));
          break;
        case 409:
          notify.error(message);
          break;
        case 422:
          notify.error(message);
          break;
        case 500:
          notify.error(transloco.translate('errors.server'));
          break;
        case 0:
          notify.error(transloco.translate('errors.offline'));
          break;
      }

      return throwError(() => err);
    })
  );
};
