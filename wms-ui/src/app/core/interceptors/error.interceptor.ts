import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../../shared/services/notification.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notify = inject(NotificationService);

  return next(req).pipe(
    catchError(err => {
      // Don't show notification for 401 (handled by auth interceptor)
      if (err.status === 401) {
        return throwError(() => err);
      }

      const message = err.error?.message || err.message || 'An unexpected error occurred';

      switch (err.status) {
        case 400:
          notify.error(message);
          break;
        case 403:
          notify.error('Access denied');
          break;
        case 404:
          notify.error('Resource not found');
          break;
        case 409:
          notify.error(message);
          break;
        case 422:
          notify.error(message);
          break;
        case 500:
          notify.error('Server error. Please try again later.');
          break;
        case 0:
          notify.error('Unable to connect to server');
          break;
      }

      return throwError(() => err);
    })
  );
};
