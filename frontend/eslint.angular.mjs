/**
 * Agentics WMS — Angular qoidalari (Wash `eslint.angular.mjs` naqshi).
 *
 * `eslint.config.mjs` dan alohida: @angular-eslint plaginlari faqat
 * nx.configs['flat/angular'] / ['flat/angular-template'] bilan ro'yxatdan o'tadi.
 *
 * Wash'dan farqi (sabab — D2, ko'chirish):
 *  - `component-max-inline-declarations` YO'Q: eski `wms-ui` da inline shablon va
 *    uslubli komponentlar bor (`notification-bell`, `limit-notice`). Ular ko'chganda
 *    faylga ajratish afzal, lekin majburiy emas.
 *  - `template/no-inline-styles` YO'Q: eski dizayn `style="color: var(--text-muted)"`
 *    kabi 60 ta inline CSS o'zgaruvchisidan foydalanadi (o'lchangan, 2026-09-10).
 *    Ularni sinfga aylantirish — qayta dizayn, ko'chirish emas.
 *  - `template/i18n` faqat MATN tugunlarini tekshiradi (`checkAttributes: false`):
 *    `placeholder="88 123 45 67"` kabi texnik namunalar eski ekranlarda ko'p.
 *    Matn tugunidagi qotirilgan so'z esa — xato (platforma standarti, D11).
 */
const MAX_FILE_LINES = ['error', { max: 500, skipBlankLines: true, skipComments: true }];

export default [
  {
    files: ['**/*.ts'],
    rules: {
      '@angular-eslint/prefer-standalone': 'error',
      '@angular-eslint/no-duplicates-in-metadata-arrays': 'error',
      '@angular-eslint/prefer-on-push-component-change-detection': 'error',
      '@angular-eslint/prefer-signals': 'error',
      '@angular-eslint/prefer-signal-model': 'error',
      '@angular-eslint/prefer-output-emitter-ref': 'error',
      '@angular-eslint/prefer-output-readonly': 'error',
      '@angular-eslint/no-input-rename': 'error',
      '@angular-eslint/no-output-rename': 'error',
      '@angular-eslint/no-inputs-metadata-property': 'error',
      '@angular-eslint/no-outputs-metadata-property': 'error',
      '@angular-eslint/no-queries-metadata-property': 'error',
      '@angular-eslint/prefer-inject': 'error',
      '@angular-eslint/use-lifecycle-interface': 'error',
      '@angular-eslint/no-empty-lifecycle-method': 'error',
      '@angular-eslint/use-injectable-provided-in': 'error',
      '@angular-eslint/component-class-suffix': 'off',
      '@angular-eslint/directive-class-suffix': 'off',
    },
  },
  {
    // Fayl hajmi — SOF kod bo'yicha; izoh sanalmaydi (izoh qarorni yozadi).
    files: ['**/*.ts'],
    ignores: ['**/*.spec.ts', '**/*.test.ts', '**/test-setup.ts'],
    rules: {
      'max-lines': MAX_FILE_LINES,
    },
  },
  {
    files: ['**/*.html'],
    rules: {
      '@angular-eslint/template/prefer-control-flow': 'error',
      '@angular-eslint/template/use-track-by-function': 'error',
      '@angular-eslint/template/no-empty-control-flow': 'error',
      '@angular-eslint/template/i18n': [
        'error',
        {
          checkId: false,
          checkText: true,
          checkAttributes: false,
        },
      ],
      '@angular-eslint/template/no-any': 'error',
      '@angular-eslint/template/no-non-null-assertion': 'error',
      '@angular-eslint/template/no-call-expression': 'off',
      '@angular-eslint/template/banana-in-box': 'error',
      '@angular-eslint/template/no-negated-async': 'error',
      '@angular-eslint/template/no-duplicate-attributes': 'error',
      '@angular-eslint/template/prefer-self-closing-tags': 'error',
      '@angular-eslint/template/eqeqeq': 'error',
      '@angular-eslint/template/alt-text': 'error',
      '@angular-eslint/template/button-has-type': 'error',
      '@angular-eslint/template/no-autofocus': 'error',
      '@angular-eslint/template/no-positive-tabindex': 'error',
    },
  },
];
