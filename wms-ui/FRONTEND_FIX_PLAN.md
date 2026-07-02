# Frontend (wms-ui) — Tuzatish va Yaxshilash Rejasi

> **Manba:** 2026-07-02 dagi to'liq audit (backend + frontend). Backend tomonida barcha tuzatishlar
> allaqachon bajarilgan (tranzaksiyalar, tenant validatsiya, policy'lar, permission enforcement,
> typed exception'lar). Bu hujjat FAQAT frontend ishlari uchun.
>
> **Ish tartibi:** Bosqichlarni tartib bilan bajaring (P0 → P1 → P2 → P3). Har bosqichdan keyin
> `ng build` xatosiz o'tishi shart. Har bir band yakunida bandning oldiga `[x]` qo'yib boring.
>
> **Konvensiyalar (CLAUDE.md):** signals + computed (BehaviorSubject yo'q), `inject()`, standalone,
> `ChangeDetectionStrategy.OnPush`, toast/confirm faqat `NotificationService` orqali, tarjimalar
> Transloco orqali (`src/assets/i18n/uz|ru|en|uz-cyrl.json`), API faqat `environment.apiUrl` orqali.

---

## Backend kontrakt faktlari (tuzatishlarda kerak bo'ladi)

- Barcha javoblar `ApiResponse<T> { success, data, message }`. Xatolar: 400 (`AppException`),
  404 (`NotFoundException`), 403 (permission/policy), 401 (autentifikatsiya) — hammasi
  `{ success: false, message }` formatida.
- **Tokenlar qat'iy ajratilgan:** asosiy JWT faqat asosiy API'da ishlaydi; `portalToken` faqat
  `/api/portal/*` da; `agentPortalToken` faqat `/api/agent-portal/*` da. Portal tokeni asosiy
  endpointga yuborilsa — 403.
- Enum'lar **raqam** bo'lib keladi: TransferType `Incoming=1, Outgoing=2, Internal=3,
  ProductionOutput=4, Return=5`; TransferStatus `Pending=1, Confirmed=2, Rejected=3, Cancelled=4`;
  ReturnReason `Expired=1, Unsold=2, Defective=3, Other=4`; PaymentMethod `Cash=1, Bank=2, Card=3`.
- **Yangi:** `CreatePaymentDto.direction?: 1 | 2` (1=In — kontragentdan olindi, 2=Out — kontragentga
  to'landi). Yuborilmasa backend qarz belgisidan o'zi topadi.
- Backend endi rad etadi (400): bo'sh items, `quantity <= 0`, `unitPrice < 0`, Internal transferda
  bir xil from/to ombor, `commissionPercent` 0–100 dan tashqarida, sotilganidan ortiq qaytarish
  (over-return), butun yozuvlarni qoplamaydigan qisman komissiya to'lovi.
- Sanalar backend'da **UTC**, `Z` belgisisiz serializatsiya qilinadi.
- Barcha ro'yxat endpointlari `?page=1&pageSize=20` qo'llaydi (default pageSize=20!).
- Mavjud BO'LMAGAN endpointlar (frontend chaqiryapti, lekin backendda yo'q — 404):
  `DELETE /finance/transactions/{id}`, `GET /counterparties/{id}`, `PUT|DELETE /locations/{id}`,
  `PUT /auth/profile`, `PUT /auth/change-password`, `PUT|DELETE /qc/parameters/{id}`.

---

## P0 — KRITIK (ishlamayotgan funksiyalar)

### 1. Kontragent portali tokeni ulanmaydi — portal butunlay buzuq
**Fayl:** `src/app/core/interceptors/auth.interceptor.ts` (12–44-qatorlar)

Hozir: faqat `req.url.includes('/agent-portal/')` tarmog'i bor. `/api/portal/*` so'rovlari asosiy
tarmoqqa tushadi → asosiy JWT ulanadi (403) yoki token umuman ulanmaydi (401). 401 esa
`authService.logout()` ni chaqiradi — portal foydalanuvchisi **asosiy** `/auth/login` ga uloqtiriladi
va asosiy sessiya o'chadi.

Qilinishi kerak:
- `/portal/` uchun alohida tarmoq (agent-portal tarmog'idan OLDIN emas, undan KEYIN, chunki
  `/agent-portal/` ham `/portal/` substring'ini o'z ichiga oladi — tekshiruvni
  `req.url.includes('/agent-portal/')` birinchi, keyin `req.url.includes('/portal/')` tartibida qiling):
  - `localStorage.getItem('portalToken')` ni Authorization headerga ulash;
  - 401 da (login so'rovidan tashqari): portal storage tozalash (`PortalService.logout()` ni inject
    qilib chaqirish) va `/portal/login` ga yo'naltirish. `AuthService.logout()` chaqirilmasin!
- Agent-portal tarmog'ida ham 401 da raw localStorage o'chirish o'rniga
  `AgentPortalService.logout()` chaqirilsin (signal'lar ham tozalansin).
- Asosiy tarmoqdagi 401-logout `/portal/` va `/agent-portal/` URL'lariga hech qachon tegmasin.

### 2. Notification polling logout'dan keyin to'xtamaydi — 401/logout sikli
**Fayllar:** `src/app/shared/services/notification-bell.service.ts` (98–114),
`src/app/core/services/auth.service.ts` (logout, 71–78), `src/app/app.ts` (33–36)

Hozir: `startPolling()` `setInterval(60000)` ochadi, `stopPolling()` hech qayerda chaqirilmaydi.
Logout'dan keyin har 60s tokensiz so'rov → 401 → yana logout → foydalanuvchi portal/boshqa sahifada
bo'lsa ham `/auth/login` ga uloqtiriladi. Qayta login'da ikkinchi interval ochiladi (double polling).

Qilinishi kerak:
- `AuthService.logout()` ichida `NotificationBellService.stopPolling()` chaqirilsin
  (circular DI bo'lsa — `injector.get()` yoki polling'ni router event bilan boshqaring).
- `startPolling()` idempotent bo'lsin: `if (this.intervalId) return;`.
- `app.ts` dagi `startPolling()` + `refreshPermissions()` faqat asosiy ilova route'larida ishlasin —
  `/portal` va `/agent-portal` URL'larida ishga tushmasin (router.url tekshiruvi).

### 3. Modul himoyasi aldanadi — xatoda hamma modul yoqiladi
**Fayl:** `src/app/core/services/tenant.service.ts` (37–77)

Hozir: `loadModules()` xato bo'lsa ham, F5 da