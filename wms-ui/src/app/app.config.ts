import { ApplicationConfig, inject, isDevMode, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MessageService, ConfirmationService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';
import { provideTransloco } from '@jsverse/transloco';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { languageInterceptor } from './core/interceptors/language.interceptor';
import { TranslocoHttpLoader } from './core/services/transloco-loader';
import { BrandingService } from './core/services/branding.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // Brendni birinchi chizishdan OLDIN qo'llaymiz. Aks holda mijoz bir lahza bizning
    // standart rangimizni, so'ng o'zinikini ko'radi — sahifa "sakragandek" bo'ladi.
    // Faqat kirgan foydalanuvchi uchun: login sahifasi doim standart ko'rinishda qoladi.
    provideAppInitializer(() => {
      if (localStorage.getItem('token')) inject(BrandingService).restore();
    }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, languageInterceptor, errorInterceptor])),
    provideAnimationsAsync(),
    providePrimeNG({
      theme: {
        preset: Aura,
        options: { darkModeSelector: '.dark-mode' }
      }
    }),
    ...provideTransloco({
      config: {
        availableLangs: ['uz', 'uz-cyrl', 'ru', 'en'],
        defaultLang: 'uz',
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader
    }),
    MessageService,
    ConfirmationService
  ]
};
