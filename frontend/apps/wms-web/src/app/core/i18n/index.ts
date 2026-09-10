import type { RootTranslationSources } from '@agentics/i18n';

import uzLatn from './uz-Latn.json';

/**
 * WMS ildiz tarjimalari — `provideI18n({ rootTranslations })` ga beriladi va
 * platforma ildizi (`common`, `errors`, `auth`, `validation`…) USTIGA chuqur
 * birlashtiriladi (mahsulot qiymati ustun).
 *
 * Tillar (D11): `uz-Latn` — manba, statik (default ham, fallback ham, `uz-Cyrl`
 * manbasi ham); `ru` — alohida chunk; `uz-Cyrl` uchun fayl YO'Q — `uz-Latn`
 * runtime'da kirillga o'giriladi (`CyrillicTranspiler`). `en` va eski qo'lda
 * yozilgan `uz-cyrl.json` o'chdi.
 *
 * Kalitlar `wms-ui/src/assets/i18n/{uz,ru}.json` dan MEXANIK ko'chirilgan
 * (kalit nomlari o'zgarmagan — ko'chirilgan shablondagi `t('warehouse.x')`
 * o'zgarishsiz ishlaydi). Istisnolar `apps/wms-web/MIGRATION.md` da.
 *
 * Hammasi ILDIZDA, feature scope'ida emas: eski ekranlar global kalitlarni
 * ishlatadi va menyu bo'lim kalitlarini (`warehouse.stockOverview`) sahifa
 * chunk'i yuklanishidan oldin ko'rsatadi.
 */
export const WMS_ROOT_TRANSLATIONS: RootTranslationSources = {
  'uz-Latn': uzLatn,
  ru: () => import('./ru.json').then((module) => module.default),
};
