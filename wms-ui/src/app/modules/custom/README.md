# Custom feature'lar — konvensiya

Bitta mijoz uchun yozilgan, boshqalarga ko'rinmaydigan imkoniyatlar shu papkada turadi.
**Alohida branch va alohida deploy yo'q** — hammasi bitta kod bazasida, feature bilan yopiladi.

## Qoidalar

1. Papka: `src/app/modules/custom/<feature-code>/`
2. Route **lazy** bo'lishi shart (`loadComponent` / `loadChildren`).
3. Route'da `featureGuard('custom.<code>')` — **majburiy**.
4. Sidebar yozuvi ham shu `featureCode` bilan shartlanadi.
5. i18n kalitlari faqat `custom.<code>.*` prefiksi bilan — umumiy bo'limlar ifloslanmasin.
6. `shared/` va boshqa umumiy komponentlarga custom mantiq **yozilmaydi**.
7. Backend tomonda `Feature.IsCustom = true`, `DefaultEnabled = false` va hech qanday
   planga kirmaydi — faqat tenant override orqali yoqiladi.

Uchinchi mijoz shu imkoniyatni so'rasa — umumiy funksiyaga ko'chiriladi va custom
yozuv o'chiriladi. Reestr: backend tomonidagi `CUSTOM_FEATURES.md`.

## Namuna

`example-feature/` — bo'sh, lekin to'liq ulangan skelet: komponent, route, guard, i18n.
Yangi custom fitcha shundan ko'chiriladi.
