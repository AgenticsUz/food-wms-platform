import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';
import { TranslocoService } from '@jsverse/transloco';

/**
 * Backend xabarlari ham konsol tilida kelsin. Aks holda interfeys o'zbekcha,
 * server xatosi inglizcha bo'lib, aralash til chiqadi.
 */
export const languageInterceptor: HttpInterceptorFn = (req, next) => {
  const lang = inject(TranslocoService).getActiveLang();
  return next(req.clone({ setHeaders: { 'Accept-Language': lang } }));
};
