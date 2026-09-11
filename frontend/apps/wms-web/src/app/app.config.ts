import {
  inject,
  isDevMode,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
  type ApplicationConfig,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';
import { provideAppConfig } from '@agentics/config';
import { AUTH_SESSION_PROFILE, provideAuth, provideSessionRestore } from '@agentics/auth';
import { provideI18n } from '@agentics/i18n';

import { appRoutes } from './app.routes';
import { provideWmsHttp } from './core/api/wms-http';
import { provideBranding } from './core/branding/branding.service';
import { WMS_ROOT_TRANSLATIONS } from './core/i18n';
import { provideWmsIdentityTenant } from './core/i18n/identity-tenant';
import { ThemeService } from './core/theme/theme.service';

/**
 * Ilova provayder zanjiri (PLATFORMA-TZ §5.3, Wash `app.config.ts` naqshi).
 *
 *  1. `provideAppConfig()` — `assets/config/app-config.json` ni bootstrapdan OLDIN
 *     yuklaydi (Identity manzili, `wms-web` mijozi, scope — bir image uch muhitda).
 *  2. `provideWmsHttp()` — paket interceptorlari + WMS konverti (`provideCoreHttp`
 *     O'RNIGA — sababi `wms-http.ts` da).
 *  3. `provideAuth()` — OIDC PKCE + BFF sessiya, proaktiv refresh, endsession bilan chiqish.
 *  4. `provideI18n()` — uz-Latn / uz-Cyrl (transliteratsiya) / ru, `Accept-Language`.
 *  5. `provideWmsIdentityTenant()` — «Kirish hisoblari» (`/settings/access`) WMS tilida.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    // `storagePrefix`: `wms.language`, `wms.auth.pkce`, `wms.branding`, `wms.theme.mode`.
    // Chiqishda paket `wms.*` ning HAMMASINI o'chiradi (til va mavzudan tashqari).
    provideAppConfig({ storagePrefix: 'wms.' }),
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(appRoutes, withComponentInputBinding()),
    provideWmsHttp(),
    // Tenant `/api/me` ichida keladi (`embedded`, TZ §5.2). Modul yopiq bo'lsa
    // `/not-in-plan` (paket sukuti `/module-disabled` — WMS'da u sahifa yo'q).
    provideAuth({ tenantSource: 'embedded', routes: { moduleDisabled: '/not-in-plan' } }),
    // Sessiya profili OSHKOR (TZ §5.3, §7a-4): kirgandan keyin `GET /api/me`.
    { provide: AUTH_SESSION_PROFILE, useValue: 'tenant' as const },
    // Sahifa yangilanganda sessiya httpOnly refresh cookie'dan tiklanadi.
    provideSessionRestore(),
    ...provideI18n({ production: !isDevMode(), rootTranslations: WMS_ROOT_TRANSLATIONS }),
    provideWmsIdentityTenant(),
    provideBranding(),
    provideAppInitializer(() => inject(ThemeService).init()),
    // Eski `wms-ui` bilan bir xil: Aura + `.dark-mode`; palitra `styles.css` dagi
    // `--p-*` o'zgaruvchilari va tenant rangi (`BrandingService`) bilan ustiga yoziladi.
    providePrimeNG({ theme: { preset: Aura, options: { darkModeSelector: '.dark-mode' } } }),
    MessageService,
    ConfirmationService,
  ],
};
