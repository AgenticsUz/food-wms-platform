import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Agent portal uses its own token, separate from tenant/counterparty auth
  if (req.url.includes('/agent-portal/')) {
    const agentToken = localStorage.getItem('agentPortalToken');
    if (agentToken) {
      req = req.clone({ setHeaders: { Authorization: `Bearer ${agentToken}` } });
    }
    return next(req).pipe(
      catchError(err => {
        if (err.status === 401 && !req.url.includes('/agent-portal/login')) {
          localStorage.removeItem('agentPortalToken');
          localStorage.removeItem('agentPortalProfile');
          router.navigate(['/agent-portal/login']);
        }
        return throwError(() => err);
      })
    );
  }

  const token = authService.token();

  if (token) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }

  return next(req).pipe(
    catchError(err => {
      if (err.status === 401) {
        authService.logout();
      }
      return throwError(() => err);
    })
  );
};
