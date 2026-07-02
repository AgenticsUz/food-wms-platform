import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { PortalService } from '../services/portal.service';
import { AgentPortalService } from '../services/agent-portal.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const portalService = inject(PortalService);
  const agentPortalService = inject(AgentPortalService);

  // Agent portal uses its own token, separate from tenant/counterparty auth
  if (req.url.includes('/agent-portal/')) {
    const agentToken = localStorage.getItem('agentPortalToken');
    if (agentToken) {
      req = req.clone({ setHeaders: { Authorization: `Bearer ${agentToken}` } });
    }
    return next(req).pipe(
      catchError(err => {
        if (err.status === 401 && !req.url.includes('/agent-portal/login')) {
          // logout() clears both the signals and localStorage, then routes to agent login
          agentPortalService.logout();
        }
        return throwError(() => err);
      })
    );
  }

  // Counterparty portal uses its own token; must NOT fall through to the main JWT
  if (req.url.includes('/portal/')) {
    const portalToken = localStorage.getItem('portalToken');
    if (portalToken) {
      req = req.clone({ setHeaders: { Authorization: `Bearer ${portalToken}` } });
    }
    return next(req).pipe(
      catchError(err => {
        if (err.status === 401 && !req.url.includes('/portal/login')) {
          // Do NOT call authService.logout() here — that would wipe the main session
          portalService.logout();
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
