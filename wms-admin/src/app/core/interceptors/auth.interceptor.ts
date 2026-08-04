import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { TranslocoService } from '@jsverse/transloco';
import { NotificationService } from '../services/notification.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const notify = inject(NotificationService);
  const transloco = inject(TranslocoService);

  const token = auth.token();
  if (token) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }

  const isLogin = req.url.includes('/auth/login');

  return next(req).pipe(
    catchError(err => {
      if (err.status === 401) {
        auth.logout();
      } else if (!isLogin) {
        const message = err.error?.message || transloco.translate('errors.unexpected');
        notify.error(message);
      }
      return throwError(() => err);
    })
  );
};
