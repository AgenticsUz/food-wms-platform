import { inject, type EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
import { IDENTITY_TENANT_OPTIONS, type IdentityTenantOptions } from '@agentics/identity-tenant';
import { LanguageService } from '@agentics/i18n';

/**
 * «Kirish hisoblari» paketining (`@agentics/identity-tenant`) tili — WMS tili.
 *
 * Nega `provideIdentityTenant({ language: () => inject(LanguageService).language() })`
 * EMAS (paket README'sidagi shakl): `provideIdentityTenant` sozlamani `useValue`
 * bilan beradi, `language` esa keyinroq paketning `computed` i ichida chaqiriladi —
 * u yerda injection konteksti yo'q va `inject()` NG0203 bilan yiqilardi. Shuning
 * uchun o'sha tokenning O'ZI factory bilan beriladi: `LanguageService` bir marta
 * shu yerda olinadi, closure esa faqat uning signalini o'qiydi — til almashsa
 * sahifa darhol yangilanadi.
 *
 * WMS tillari paketnikiga aynan mos (`uz-Latn` | `uz-Cyrl` | `ru`), moslash kerak emas.
 */
export function provideWmsIdentityTenant(): EnvironmentProviders {
  return makeEnvironmentProviders([
    {
      provide: IDENTITY_TENANT_OPTIONS,
      useFactory: (): IdentityTenantOptions => {
        const language = inject(LanguageService).language;
        return { language: () => language() };
      },
    },
  ]);
}
