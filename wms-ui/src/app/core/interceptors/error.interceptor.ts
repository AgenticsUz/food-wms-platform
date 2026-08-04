import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../../shared/services/notification.service';

const SUBSCRIPTION_PAGE = '/settings/subscription';

/** Obuna bloki (402) — sabab bo'yicha tarjima kaliti. */
const BLOCK_KEYS: Record<string, string> = {
  subscription_suspended: 'errors.subscriptionSuspended',
  trial_expired: 'errors.trialExpired',
  tenant_inactive: 'errors.tenantInactive'
};

/** Plan limiti (402) — sabab bo'yicha tarjima kaliti. */
const LIMIT_KEYS: Record<string, string> = {
  limit_users: 'errors.limitUsers',
  limit_warehouses: 'errors.limitWarehouses',
  limit_transfers: 'errors.limitTransfers'
};

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
          const blockKey = BLOCK_KEYS[code];
          if (blockKey) {
            // Obuna to'xtatilgan / muddati o'tgan — sababni Obuna sahifasida ko'rsatamiz.
            // Bu sahifaning o'zi enforcement'dan ozod, ya'ni ochiladi.
            notify.error(transloco.translate(blockKey));
            if (!router.url.startsWith(SUBSCRIPTION_PAGE)) {
              router.navigate([SUBSCRIPTION_PAGE]);
            }
            break;
          }
          const limitKey = LIMIT_KEYS[code];
          // Limit oshgan — foydalanuvchi shu sahifada qoladi, faqat sababni ko'radi
          notify.error(limitKey
            ? `${transloco.translate(limitKey)} — ${message}`
            : message);
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
