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
 *
 * F9: WMS `client` va `agent` rollarini ham e'lon qiladi (kabinet). Paketning sukut
 * yorliqlarida ular yo'q va ro'yxatda xom kod ko'rinardi — shuning uchun yorliq
 * shu yerda beriladi. Matn «(kabinet)» bilan: bu ro'yxat XODIMLAR ro'yxati ham,
 * ya'ni admin ikkisini adashtirmasin.
 */
export function provideWmsIdentityTenant(): EnvironmentProviders {
  return makeEnvironmentProviders([
    {
      provide: IDENTITY_TENANT_OPTIONS,
      useFactory: (): IdentityTenantOptions => {
        const language = inject(LanguageService).language;
        return { language: () => language(), roleLabels: WMS_ROLE_LABELS };
      },
    },
  ]);
}

/** Kabinet rollari — `WmsSystemRoles.DisplayName` bilan bir xil ma'no. */
const WMS_ROLE_LABELS = {
  client: {
    'uz-Latn': 'Mijoz (kabinet)',
    'uz-Cyrl': 'Мижоз (кабинет)',
    ru: 'Клиент (кабинет)',
  },
  agent: {
    'uz-Latn': 'Agent (kabinet)',
    'uz-Cyrl': 'Агент (кабинет)',
    ru: 'Агент (кабинет)',
  },
} as const;
