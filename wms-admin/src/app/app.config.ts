import { ApplicationConfig, isDevMode, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideTransloco } from '@jsverse/transloco';
import { TranslocoHttpLoader } from './core/transloco-loader';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MessageService, ConfirmationService } from 'primeng/api';
import { providePrimeNG } from 'primeng/config';
import { definePreset } from '@primeng/themes';
import Aura from '@primeng/themes/aura';

/**
 * Aura'ning standart primary rangi — emerald. Uni pistachio bilan almashtiramiz,
 * aks holda tugmalar va paginator brend rangidan farq qiladi: `styles.scss` dagi
 * `--p-*` o'zgaruvchilari presetning o'z qiymatlaridan keyin qo'llanmaydi.
 */
const WmsPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#f0f7ec',
      100: '#ddedce',
      200: '#b8d99c',
      300: '#9ec87b',
      400: '#7eb35a',
      500: '#5e9540',
      600: '#4a7a32',
      700: '#355823',
      800: '#2a4419',
      900: '#1e3011',
      950: '#132009'
    }
  }
});

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { languageInterceptor } from './core/interceptors/language.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, languageInterceptor])),
    provideAnimationsAsync(),
    providePrimeNG({
      theme: {
        preset: WmsPreset,
        options: { darkModeSelector: '.dark-mode' }
      }
    }),
    provideTransloco({
      config: {
        availableLangs: ['uz', 'ru', 'en'],
        defaultLang: 'uz',
        fallbackLang: 'uz',
        reRenderOnLangChange: true,
        prodMode: !isDevMode()
      },
      loader: TranslocoHttpLoader
    }),
    MessageService,
    ConfirmationService
  ]
};
