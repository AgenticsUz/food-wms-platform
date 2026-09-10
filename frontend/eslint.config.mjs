import nx from '@nx/eslint-plugin';

/**
 * Agentics WMS — workspace ESLint bazasi.
 *
 * Wash (`agentics-wash/frontend/eslint.config.mjs`) naqshidan olingan, lekin
 * IKKI farq bilan va ikkalasi ham ataylab (PLATFORMA-TZ §7·F6.2 D2 — «ko'chirish,
 * qayta yozish emas»):
 *
 *  1. PrimeNG, ApexCharts va primeicons HAMMA JOYDA ruxsat. Wash'da ular faqat
 *     `libs/shared/ui` o'ramlari ichida — chunki Wash o'z dizayn tizimini
 *     noldan qurgan. WMS'ning 60+ ekrani esa `wms-ui` dan PrimeNG'ni to'g'ridan-
 *     to'g'ri ishlatgan holda ko'chadi; har komponentga o'ram yozish ko'chirishni
 *     qayta yozishga aylantirardi.
 *  2. Komponentda `subscribe()` TAQIQLANMAYDI. Eski ekranlar `api.get(...).subscribe`
 *     naqshida yozilgan va ular bir-bir ko'chadi. Yangi kodda `toSignal()` /
 *     `firstValueFrom()` afzal, lekin buni lint emas, ko'rib chiqish talab qiladi.
 *
 * Qolgan taqiqlar Wash bilan BIR XIL: NgModule yo'q, `any` yo'q, non-null `!` yo'q,
 * `innerHTML` yo'q, `console.log` yo'q, HttpClient faqat servislarda.
 */

const HTTP_PATTERNS = [
  {
    group: ['@angular/common/http', '@angular/common/http/*'],
    message:
      "HttpClient faqat `core/` va `*.service.ts` ichida. Komponent servis orqali gaplashadi — servis esa `ApiService` (core/api) orqali: WMS javob konverti, 402/403 kodlari va `warning` toasti bitta joyda.",
  },
];

const NGMODULE_PATHS = [
  {
    name: '@angular/core',
    importNames: ['NgModule'],
    message: 'NgModule butun loyihada taqiqlangan — faqat standalone komponentlar.',
  },
];

const NO_INNER_HTML = {
  selector: "MemberExpression[property.name='innerHTML']",
  message: 'innerHTML taqiq — XSS xavfi.',
};

export default [
  ...nx.configs['flat/base'],
  ...nx.configs['flat/typescript'],
  ...nx.configs['flat/javascript'],
  {
    // src/index.html — brauzer hujjati, Angular shabloni emas.
    ignores: ['**/dist', '**/out-tsc', '**/.angular', '**/coverage', '**/src/index.html'],
  },
  {
    files: ['**/*.ts', '**/*.tsx', '**/*.js', '**/*.jsx'],
    rules: {
      '@nx/enforce-module-boundaries': [
        'error',
        {
          enforceBuildableLibDependency: true,
          allow: ['^.*/eslint(\\.base)?\\.config\\.[cm]?[jt]s$'],
          depConstraints: [{ sourceTag: '*', onlyDependOnLibsWithTags: ['*'] }],
        },
      ],
    },
  },
  {
    files: ['**/*.ts', '**/*.tsx', '**/*.mts', '**/*.cts'],
    rules: {
      '@typescript-eslint/no-explicit-any': 'error',
      '@typescript-eslint/no-non-null-assertion': 'error',
      'no-restricted-imports': ['error', { paths: NGMODULE_PATHS, patterns: HTTP_PATTERNS }],
      'no-restricted-syntax': ['error', NO_INNER_HTML],
      eqeqeq: ['error', 'smart'],
      'prefer-const': 'error',
      'no-console': ['error', { allow: ['warn', 'error'] }],
    },
  },
  // HttpClient ruxsat etilgan joylar: HTTP qatlami (`core/`) va domen servislari.
  {
    files: ['**/src/app/core/**/*.ts', '**/*.service.ts'],
    rules: {
      'no-restricted-imports': ['error', { paths: NGMODULE_PATHS }],
    },
  },
  // Testlar va vositalar.
  {
    files: ['**/*.spec.ts', '**/*.test.ts', '**/test-setup.ts', '**/*.config.mjs', '**/*.config.js'],
    rules: {
      'no-restricted-syntax': 'off',
      'no-console': 'off',
      'no-restricted-imports': ['error', { paths: NGMODULE_PATHS }],
    },
  },
];
