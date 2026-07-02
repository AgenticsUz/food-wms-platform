# WMS-UI — Frontend tuzatish rejasi (Frontend Fix Plan)

> **Maqsad:** wms-ui (Angular 21, standalone, signals, zoneless, PrimeNG 21, Tailwind v4, Transloco) frontendidagi haqiqiy nuqsonlarni tuzatish.
> **Kontekst:** Backend (wms-api) yaqinda audit qilinib tuzatildi — endi u qat'iy validatsiya qiladi, permission tekshiradi, portal/agent tokenlarini asosiy API'da rad etadi (403). Quyidagi kamchiliklarning KO'PCHILIGI backend o'zgarishidan OLDIN ham mavjud edi.
> **Muhim:** Har bir bo'limdan keyin `ng build` ishlashini tekshiring. O'zgarishlardan keyin `npm run build` (yoki `ng build`) xatosiz o'tishi shart.

## Loyiha yo'llari
- Frontend root: `D:\C disk\projects\Personal-Project\projects\wms\wms-ui`
- Backend root (reference, o'zgartirilmaydi): `D:\C disk\projects\Personal-Project\projects\wms\wms-api`
- i18n fayllar: `src/assets/i18n/{uz,ru,en,uz-cyrl}.json` (Transloco loader shu yerdan o'qiydi — `dist/` ichidagi eski nusxaga tegmang)

## Konventsiyalar (CLAUDE.md dan — QAT'IY amal qiling)
- Faqat signals (`signal`, `computed`, `effect`) — BehaviorSubject ishlatmang
- `inject()` — konstruktor DI emas
- Barcha komponentlar `standalone: true`, `ChangeDetectionStrategy.OnPush`
- Toast/confirm faqat `NotificationService` orqali — `MessageService` to'g'ridan-to'g'ri emas
- Barcha matnlar Transloco orqali (`t('kalit')`) — hardcoded matn yo'q
- Pul: `decimal` backendda; frontendda `number`, lekin formatlashda ehtiyot
- Sanalar: backend UTC, foydalanuvchi UTC+5 (O'zbekiston)
- API javoblari: `ApiResponse<T> { success, data, message }`

## Backend kontrakt faktlari (tekshirilgan)
- Transfer turlari: `Incoming=1, Outgoing=2, Internal=3, ProductionOutput=4, Return=5`
- Transfer statuslari: `Pending=1, Confirmed=2, Rejected=3, Cancelled=4`
- ReturnReason: `Expired=1, Unsold=2, Defective=3, Other=4`
- CounterpartyType: `Supplier=1, Client=2, Both=3`
- Backend endi RAD ETADI (400): bo'sh items, `quantity<=0`, `unitPrice<0`, bir xil ombor Internal transfer, `commissionPercent` 0-100 tashqarisi, sotilganidan ortiq qaytarish (over-return), butun komissiya yozuvini qoplamaydigan qisman payout
- `CreatePaymentDto` endi ixtiyoriy `direction` (1=In/kelgan, 2=Out/to'langan) maydoniga ega
- Endpoint YO'Q (UI chaqiryapti, lekin backendda mavjud emas): `DELETE /finance/transactions/{id}`, `GET /counterparties/{id}`, `PUT /locations/{id}`, `DELETE /locations/{id}`, `PUT /auth/profile`, `PUT /auth/change-password`, `PUT /qc/parameters/{id}`, `DELETE /qc/parameters/{id}`
- Endpoint BOR (UI ishlatmayapti): `DELETE /transfers/{id}` (pending transferni cancel qilish)

---

# FAZA 1 — KRITIK (hozir buzuq funksiyalar)

## 1.1 — Kontragent portali tokeni ulanmaydi (ENG KRITIK)
**Fayl:** `src/app/core/interceptors/auth.interceptor.ts`
**Muammo:** Interceptor'da faqat `req.url.includes('/agent-portal/')` uchun tarmoq bor. `/portal/` uchun tarmoq YO'Q, shuning uchun `/api/portal/me|transfers|finance|payments` so'rovlariga `portalToken` ulanmaydi — o'rniga asosiy JWT (yoki hech narsa) ketadi. Backend `PortalController` `[Authorize(Policy = "PortalOnly")]` bo'lgani uchun 403 yoki 401 qaytadi.
**Natija:** Portal sahifalari bo'sh, "Access denied" toast spam, yoki token muddati o'tganda kontragent asosiy login sahifasiga uloqtiriladi.
**Tuzatish:**
- `/agent-portal/` tarmog'iga o'xshash `/portal/` tarmog'i qo'shing:
  - Shart: `req.url.includes('/portal/') && !req.url.includes('/agent-portal/')`
  - `portalToken` ni localStorage'dan olib `Authorization: Bearer` header qo'shing
  - 401 xatoda: portal storage (`portalToken`, `portalCounterparty`) tozalash va `/portal/login` ga navigate — `authService.logout()` CHAQIRMANG
  - `/portal/login` URL'ini logout ishlovidan chiqaring
**Bog'liq:** 1.2 bilan birga qiling.

## 1.2 — Portal/agent 401 asosiy sessiyani o'chiradi
**Fayl:** `src/app/core/interceptors/auth.interceptor.ts` (~37-44 qatorlar)
**Muammo:** Portal so'rovlari umumiy `catchError`ga tushadi: `if (err.status === 401) { authService.logout(); }` → asosiy tenant tokenini o'chirib `/auth/login` ga yuboradi.
**Tuzatish:** Portal URL 401 → `PortalService.logout()` / `/portal/login`; agent-portal URL 401 → `AgentPortalService.logout()` / `/agent-portal/login`. Faqat asosiy API 401 → `authService.logout()`.
**Qo'shimcha (14-topilma):** agent-portal 401 ishlovi hozir localStorage'ni tozalaydi, lekin `AgentPortalService` signallarini emas — `AgentPortalService.logout()` chaqiring (raw localStorage.remove o'rniga).

## 1.3 — Asosiy app auth side-effektlari portal route'larida ishlaydi
**Fayl:** `src/app/app.ts` (~33-36 qatorlar)
**Muammo:** Har boot'da `if (this.authService.isAuthenticated())` → `bellService.startPolling()` va `refreshPermissions()` ishga tushadi, URL'dan qat'i nazar. Portal foydalanuvchisida eski/muddati o'tgan asosiy token bo'lsa, fon 401 → logout → `/auth/login`.
**Tuzatish:** Bu chaqiruvlarni route'ga bog'lang (`/portal` va `/agent-portal` uchun o'tkazib yuboring) YOKI asosiy shell komponentiga ko'chiring (portal/agent layoutlari alohida).

## 1.4 — Notification polling logout'da to'xtamaydi (401 sikli)
**Fayllar:** `src/app/core/services/notification-bell.service.ts` (~98-114), `src/app/core/services/auth.service.ts` logout (~71-78)
**Muammo:** `startPolling()` `setInterval(..., 60000)` qiladi, lekin `stopPolling()` hech qayerda chaqirilmaydi. Logout'dan keyin interval davom etadi → har 60s `GET notifications/unread-count` tokensiz → 401 → logout → `/auth/login`. F5 + qayta login = ikkita parallel interval.
**Tuzatish:**
- `AuthService.logout()` da `bellService.stopPolling()` chaqiring (inject qiling)
- `startPolling()` ni idempotent qiling: `if (this.intervalId) return;` yoki avval `clearInterval`

## 1.5 — Modul himoyasi aldanadi (barcha modullar yoqiladi)
**Fayl:** `src/app/core/services/tenant.service.ts` (~37-77)
**Muammo:** `loadModules()` xato fallback va `restoreModules()` (F5'da `auth.guard.ts:15` dan) `enableAllForDev()` chaqiradi — barcha 9 modulni yoqadi. Tenant uchun FINANCE o'chirilgan bo'lsa ham, so'rov bir marta xato bersa foydalanuvchi Finance modulini ko'radi va kiradi.
**Tuzatish:** Xatoda bo'sh ro'yxatga (yoki faqat saqlangan ro'yxatga) qayting; `enableAllForDev()` ni faqat `isDevMode()` ichida chaqiring.

## 1.6 — Modullar login'dan keyin qayta yuklanmaydi (F5'da eskirgan)
**Fayllar:** `src/app/core/services/tenant.service.ts`, `src/app/core/guards/auth.guard.ts`
**Muammo:** `loadModules()` faqat login va settings sahifasidan chaqiriladi; `authGuard` faqat localStorage'dan tiklaydi. Admin modulni o'chirsa, foydalanuvchi F5'da 7 kun davomida uni ko'raveradi. Race: birinchi login'dan keyin `loadModules()` fire-and-forget, `moduleGuard` javob kelmasdan foydalanuvchini `/dashboard` ga qaytaradi.
**Tuzatish:** `authGuard` fon rejimda `loadModules()` refresh qilsin (`refreshPermissions()` kabi), yoki signal bo'sh bo'lsa guard modullarni kutsin.

## 1.7 — 5 ta "o'lik" endpoint (doim 404)
Backend'da mavjud bo'lmagan endpointlarni chaqiruvchi UI amallari. Har biri uchun: **yo tugmani/sahifani olib tashlang, yo backend'ga endpoint qo'shing.** Tavsiya: bu MD'ni backend jamoasi bilan ko'rib, qaysi biri kerakligini hal qiling. Hozircha UI'ni backend bilan moslash uchun eng oson yo'l — mavjud bo'lmagan amallarni yashirish/olib tashlash.

| # | UI fayl | Chaqirilayotgan endpoint | Tavsiya |
|---|---------|--------------------------|---------|
| a | `core/services/finance.service.ts:16` + `modules/finance/transactions/transactions.component.ts:154-164` + `.html:75` | `DELETE /finance/transactions/{id}` | Tugmani olib tashlang YOKI backend'ga qo'shing. (Tugmada `*hasPermission` guard ham yo'q) |
| b | `core/services/counterparty.service.ts:19-21` + `modules/counterparties/detail/counterparty-detail.component.ts:43-51` | `GET /counterparties/{id}` | Detail ma'lumotini list'dan oling YOKI backend'ga qo'shing |
| c | `core/services/warehouse.service.ts:85-91` + `modules/warehouse/locations/locations.component.ts:75-91` | `PUT`/`DELETE /locations/{id}` | Edit/delete tugmalarini olib tashlang YOKI backend'ga qo'shing |
| d | `core/services/settings.service.ts:49-50` + `modules/settings/profile/profile.component.ts` | `PUT /auth/profile`, `PUT /auth/change-password` | **Profil sahifasi to'liq o'lik.** Backend'ga qo'shish tavsiya etiladi (foydalanuvchi o'z parolini o'zgartira olishi kerak) |
| e | `core/services/settings.service.ts:45-46` + `modules/settings/qc-parameters/` | `PUT`/`DELETE /qc/parameters/{id}` | Edit/delete olib tashlang YOKI backend'ga qo'shing |

## 1.8 — `environment.prod.ts` da hardcoded HTTP URL
**Fayl:** `src/environments/environment.prod.ts` (3-qator)
**Muammo:** `apiUrl: 'http://api.warehouse-system.uz/api'` — sayt HTTPS'da bo'lsa mixed-content bloklanadi; CLAUDE.md bo'yicha `/api` (relative, nginx orqasida) bo'lishi kerak.
**Tuzatish:** `apiUrl: '/api'` (yoki `https://...`).

---

# FAZA 2 — NOTO'G'RI DATA / BUZUQ BINDING

## 2.1 — Ombor harakatlari (movements) sahifasi bo'sh
**Fayllar:** `modules/warehouse/movements/movements.component.{ts,html}`, `core/services/warehouse.service.ts:72-74`
**Muammo:** Sahifa `TransferDto[]` ni backend qaytarmaydigan `StockMovement` shakliga map qiladi (`m.productName`, `m.quantity`, `m.date`). `movements.component.ts:48-50` da `type === 'Incoming'` (string) taqqoslaydi, enum esa raqam bo'lib keladi. Jadval bo'sh, raqamli badge'lar.
**Tuzatish:** `transfer.items` ni client-side flatten qiling; raqamli enumlarni map qiling (helper 4.1 dan foydalaning).

## 2.2 — Dashboard donut diagrammasi bo'sh
**Fayllar:** `modules/dashboard/dashboard.component.ts:187-189`, `core/models/analytics.model.ts:51-55`
**Muammo:** `d.count`/`d.productType` o'qiydi, backend `ProductDistributionDto` esa `{ productName, type (number), totalStock, percentage }` qaytaradi → series/labels `undefined`.
**Tuzatish:** Model va map'ni to'g'rilang: `productName`, `totalStock`/`percentage` ishlating; `type` ni product turi nomiga map qiling.

## 2.3 — Dashboard "So'nggi transferlar"da raqam ko'rinadi
**Fayl:** `modules/dashboard/dashboard.component.html:226-231`
**Muammo:** `[status]="transfer.status"` = `2` (raqam), `status-badge.component.ts:13-39` string kalit kutadi → kulrang "2" pill. `itemCount` maydoni yo'q.
**Tuzatish:** Status raqamini string kalitga map qiling (helper 4.1); `items.length` ishlating.

## 2.4 — Kontragent detail: noto'g'ri type/status + butun tenant transferlari
**Fayllar:** `modules/counterparties/detail/counterparty-detail.component.html:74,77`, `core/services/counterparty.service.ts:43-45`
**Muammo:** `t.typeName ?? t.type` va `[status]="t.statusName ?? t.status"` — backend `TransferDto`da `typeName/statusName` yo'q, raqam ko'rinadi. Bundan tashqari transfer tarixi `counterpartyId` ni query param qilib yuboradi, backend uni e'tiborsiz qoldiradi → HAR kontragent sahifasida BUTUN tenant transferlari ko'rinadi (noto'g'ri data).
**Tuzatish:** type/status'ni helper bilan map qiling; transfer ro'yxatini backend `GET /transfers?...` filtri bilan (agar counterpartyId filtri bo'lsa) yoki client-side filter bilan to'g'rilang. **Backend `/transfers` counterpartyId filtrini qo'llab-quvvatlaydimi — tekshiring.**

## 2.5 — Mahsulotlar ro'yxati 20 tadan keyin ko'rinmaydi
**Fayl:** `modules/products/product-list/product-list.component.ts:72-90`
**Muammo:** `pageSize` yuborilmaydi; backend default 20 Skip/Take. Client-side paginator/filter faqat birinchi 20 ustida ishlaydi.
**Tuzatish:** Katta `pageSize` yuboring (masalan 1000) YOKI server-side paginatsiya qiling. Xuddi shu xavf: `transfer-list.component.ts:68` (`pageSize: 100` hardcoded — ko'p transferli tenantda muammo).

---

# FAZA 3 — SANA / VAQT ZONASI (backend UTC, foydalanuvchi UTC+5)

## 3.1 — `toISOString()` bir kun oldinga siljitadi (SISTEMLI)
**Muammo:** `Date.toISOString().split('T')[0]` mahalliy yarim tunni oldingi UTC kuniga aylantiradi.
**Joylar:**
- `modules/transfers/transfer-list/transfer-list.component.ts:71-72,123-124`
- `modules/finance/transactions/transactions.component.ts:83-84,182-183`
- `modules/warehouse/movements/movements.component.ts:35-36`
- `modules/dashboard/dashboard.component.ts:264-266` ("Bugun" filtri aslida kechadan boshlanadi)
- **ENG YOMONI (yozuv):** `modules/kpi/plans/plans.component.ts:101` va `modules/kpi/actuals/actuals.component.ts:119` — `f.date.toISOString()` yuboradi → har plan/actual bir kun oldin saqlanadi, plan-vs-actual grafigi noto'g'ri juftlanadi.
**Tuzatish:** Sana-only stringni mahalliy komponentlardan yasang. Umumiy helper qo'shing (masalan `src/app/shared/utils/date.util.ts`):
```ts
export function toLocalDateString(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}
```
Barcha `toISOString().split('T')[0]` ni shu bilan almashtiring.

## 3.2 — UTC timestamplar mahalliy deb o'qiladi
**Fayl:** `shared/components/notification-bell/notification-bell.component.ts:150-159` (va attendance check-in/out, `createdAt` ustunlar)
**Muammo:** Backend `Z` suffiksisiz DateTime serialize qiladi; `new Date(dateStr)` uni mahalliy deb o'qiydi → "hozirgina" bildirishnoma "5 soat oldin".
**Tuzatish:** Backend'dan kelgan sanaga `Z` qo'shing (agar yo'q bo'lsa) yoki backend UTC ekanini bilib to'g'ri parse qiling. Helper: `parseUtc(s: string) => new Date(s.endsWith('Z') ? s : s + 'Z')`.

---

# FAZA 4 — FORMA VALIDATSIYASI (backend endi rad etadigan qiymatlar)

## 4.0 — Umumiy: takroriy toast + yashirin sabab (SISTEMLI)
**Fayl:** `core/interceptors/error.interceptor.ts:16-20` allaqachon backend 400 xabarini ko'rsatadi. Lekin deyarli har komponent error callback'i YANA generic inglizcha toast qo'shadi:
- `modules/transfers/transfer-create/transfer-create.component.ts:289`
- `modules/transfers/transfer-detail/transfer-detail.component.ts:67,83`
- `modules/production/order-detail/order-detail.component.ts:96,110,154`
- va boshqalar (24-topilmaga qarang)
**Natija:** Foydalanuvchi ikkita toast oladi, biri foydasiz ("Insufficient stock..." + "Failed to...").
**Tuzatish:** Interceptor allaqachon ishlov beradigan HTTP xatolar uchun komponentdagi generic toastni olib tashlang (yoki `err.error?.message` uzating). **Qaror:** interceptor xabar ko'rsatishini yagona manba qilib belgilang; komponentlar faqat non-HTTP holatlar uchun toast qilsin.

## 4.1 — Enum → nom map helperi (takrorlanishni yo'q qilish)
**Muammo:** `getStatusName/getStatusKey/getTypeName` transfer-list, transfer-detail, order komponentlarida ko'chirilgan; movements/dashboard/counterparty-detail raqam ko'rsatadi.
**Tuzatish:** Yagona helper yarating (`src/app/shared/utils/transfer-enums.ts`) — status/type raqamini Transloco kalitiga aylantirsin:
```ts
export const TRANSFER_STATUS_KEY: Record<number, string> = {
  1: 'status.pending', 2: 'status.confirmed', 3: 'status.rejected', 4: 'status.cancelled'
};
export const TRANSFER_TYPE_KEY: Record<number, string> = {
  1: 'transfer.type.incoming', 2: 'transfer.type.outgoing', 3: 'transfer.type.internal',
  4: 'transfer.type.productionOutput', 5: 'transfer.type.return'
};
```
Barcha joyda ishlating (2.1, 2.3, 2.4 shu bilan hal bo'ladi).

## 4.2 — Transfer-create validatsiyalari
**Fayllar:** `modules/transfers/transfer-create/transfer-create.component.{ts,html}`
- **Bir xil ombor Internal (`ts:254-256`):** Internal transferda from/to bir xil ombor tanlash mumkin — client-side tekshiruv yo'q. Qo'shing: `from === to` bo'lsa xato ko'rsating, submit'ni bloklang.
- **Manfiy narx (`html:146-147`, `ts:163-180`):** unit price input'da `[min]="0"` yo'q, `addItem()` faqat `qty <= 0` tekshiradi. `[min]="0"` qo'shing va `unitPrice < 0` ni bloklang.
- **Agent tanlanmagan (`ts:263-271`):** "via agent" yoqilgan-u agent tanlanmagan bo'lsa `submit()` `agentId` ni tekshirmaydi — transfer agentsiz jimgina yaratiladi (komissiya yozilmaydi). Validatsiya qo'shing.

## 4.3 — Agent payout validatsiyasi
**Fayllar:** `modules/agents/agent-detail/agent-detail.component.{ts,html}` (~132-151, html:142-143)
**Muammo:** Payout dialog istalgan summani qabul qiladi, lekin backend butun komissiya yozuvini qoplamaydigan qisman summani rad etadi. Error handler faqat generic "Failed to pay commission" ko'rsatadi.
**Tuzatish:** To'lanmagan `commissionAmount`larning prefix-sum'lariga qarab validatsiya qiling, `[max]` bilan cheklang, `err.error.message` ni ko'rsating.

## 4.4 — Recipe-create ingredientlari jimgina tushib qoladi
**Fayl:** `modules/production/recipe-create/recipe-create.component.ts:158`
**Muammo:** `unitId`/`productId` yo'q inputlar DTO'dan jimgina filtrlanadi, `quantity: 0` qatorlar o'tadi — retsept ingredientsiz saqlanishi mumkin.
**Tuzatish:** To'liq bo'lmagan input qatorlarini validatsiya bilan bloklang (xato ko'rsating).

## 4.5 — Boshqa forma/data muammolari
- `modules/settings/modules/modules.component.ts:34-37`: `currentUser()` null bo'lsa `loadModules()` erta qaytadi, `loading` `true` qoladi → doimiy spinner. Tuzating.
- `modules/kpi/attendance/attendance.component.ts:84-90`: worker dropdown `GET /users` chaqiradi (`settings.users` permission kerak). KPI-only xodim jim 403 oladi, bo'sh ro'yxat. Alohida endpoint yoki error ishlovi qo'shing.
- `modules/settings/users/users.component.ts:136-141`: `isActive` create'da yuboriladi, lekin backend `CreateUserDto`da bunday maydon yo'q — toggle jim e'tiborsiz. UI'dan olib tashlang yoki backend'ga qo'shing.
- `shared/components/phone-input/phone-input.component.ts:27`: `maxlength="9"` yopishtirilgan `+998881234567` ni digit-strip'dan oldin kesadi → noto'g'ri raqam. `maxlength` ni olib tashlang yoki oshiring.

---

# FAZA 5 — i18n va DARK MODE

## 5.1 — Yetishmayotgan tarjima kalitlari
**Muammo:** `import.*` kalitlari (`shared/components/import-button/import-button.component.html:4-68` da ishlatiladi) `uz.json`/`ru.json`da bor, lekin `en.json` va `uz-cyrl.json`da YO'Q. Shu 9 kalit: `import.template, import.downloadTemplate, import.importExcel, import.importing, import.result, import.totalRows, import.imported, import.errors, import.success`. (uz=437, ru=437, en=428, uz-cyrl=428)
**Tuzatish:** `en.json` va `uz-cyrl.json`ga shu 9 kalitni qo'shing (mos tarjima bilan).

## 5.2 — Hardcoded matnlar (SISTEMLI, mixed-language UI)
Barchasini Transloco kalitlariga o'tkazing:
- `modules/auth/login/login.component.ts:47` (o'zbekcha), `:42` (inglizcha)
- `modules/portal/login/portal-login.component.ts:33,41,44,49` (inglizcha)
- `modules/agent-portal/login/agent-portal-login.component.ts:33,41,44,49` (inglizcha)
- Portal status/tiplari: `portal-dashboard.component.ts:55-68`, `portal-transfers.component.ts:58-76`, `portal-transfer-detail.component.ts:45-63` — `'Pending'/'Confirmed'/...` raw. Agent portal to'g'ri qiladi (`t('status.'+...)`) — o'shanga taqlid qiling.
- `notify.*`/confirm matnlari: "Transfer created", "Confirm this transfer?...", `finance/debts/debts.component.ts:52-54` ("They owe us"/"We owe them")
- Jadval body type/status: `transfer-list.component.ts:86-119`, `transfer-detail.component.ts:92-121` (inglizcha, filtrlar esa tarjimalangan)
- Batch expiry: `warehouse/batches/batches.component.ts:120-128`
- `NotificationService` summary: "Success"/"Error", confirm sarlavhalari (`shared/services/notification.service.ts:10-40`)
- `users.component.ts:107` (o'zbekcha)

## 5.3 — Dark mode PrimeNG tokenlari
**Fayl:** `src/styles.scss` (~170-236)
**Muammo:** `:root` blokida light qiymatlar hardcoded (`--p-content-background: #ffffff`, `--p-inputtext-background: #ffffff`, `--p-dialog-background: #ffffff`, `--p-drawer-background: #ffffff`, striped `--color-cream-50`). `.dark-mode` uchun hech qanday `--p-*` qayta e'lon yo'q → dark mode'da dialog/input/drawer/striped qatorlar oq/cream qoladi.
**Tuzatish:** `.dark-mode { --p-content-background: ...; --p-inputtext-background: ...; ... }` bloki qo'shing (light overridelarni oynadagidek aks ettiring).
**Bog'liq (17-topilma):** `styles.scss:117/279` vs 296/322/358/379/424 — `.dark` alias 7 dark blokdan faqat 2 tasida qo'llanadi. `.dark` ni butunlay olib tashlang yoki hamma joyda izchil qo'llang (PrimeNG konfiguratsiyasi `.dark-mode` ishlatadi).

## 5.4 — Login formalari Enter'da submit bo'lmaydi
**Fayllar:** `modules/portal/login/portal-login.component.html:8-18`, `modules/agent-portal/login/agent-portal-login.component.html:8-18`
**Muammo:** Bare `<div class="login-form">` va faqat `(onClick)` p-button — `<form (ngSubmit)>` yo'q (asosiy login'da bor). Enter bosilganda hech nima bo'lmaydi.
**Tuzatish:** `<form (ngSubmit)="login()">` ga o'rang, button `type="submit"`, ngModel inputlariga `name` qo'shing.

---

# FAZA 6 — NICE-TO-HAVE (ixtiyoriy, vaqt bo'lsa)

- **`PaymentCreateDto` ga `direction` qo'shish** (`finance.model.ts:38-44`, `payments.component.ts`): 1=In/2=Out select qo'shing — suppliergaTO'LANGAN vs kelgan to'lovni ajratish. Backend allaqachon qo'llab-quvvatlaydi.
- **`cancelTransfer` o'lik kod** (`transfer.service.ts:29`): pending transferda Cancel tugmasi qo'shing (backend `DELETE /transfers/{id}` bor).
- **`Both` (3) kontragentlar ko'rinmaydi** (`client-list.component.ts:62`, `supplier-list.component.ts:51`): exact-match type filtri — `Both`ni ikkala ro'yxatda ko'rsatish uchun OR filtri kerak.
- **Katta takrorlanish:** `client-list` vs `supplier-list` (~90% bir xil, drift boshlangan — supplier save `agentId`ni tashlab ketyapti); `EmptyStateComponent` o'lik kod, har jadval inline empty-state takrorlaydi.
- **KPI efficiency radial** (`kpi-dashboard.component.ts:43-47`): kunlik %larni averaging qiladi, `sum(actual)/sum(planned)*100` bo'lishi kerak.
- **Status-badge noto'g'ri ishlatish:** payment methods, attendance methods, debt turlari, qc types uchun approval rangli badge (chalkash). Alohida rang sxemasi qiling.
- **UTC/CD/UX mayda:** dashboard "This week" yakshanbadan boshlanadi; shift vaqtlari free-text input; transfer-detail'da "not found" state yo'q; `agent-detail.component.html:106` har qatorda `statusOptionsFor(t)` chaqiradi (memoize); autocomplete hintlari (`autocomplete="current-password"/"tel"`) yo'q.
- **Duplicate startup calls:** `app.ts:35` va `auth.guard.ts:18` ikkalasi ham `refreshPermissions()` chaqiradi (bittasini qoldiring); `auth.service.ts` `loadPermissions()` va `refreshPermissions()` bayt-bayt bir xil (bittasini o'chiring).
- **`theme.service.ts:3`:** `declare const ApexCharts` global'ga tayanadi; `import ApexCharts from 'apexcharts'` qiling.
- **Header lang** (`header.component.ts:24`): `langChanges$` + AsyncPipe → `toSignal()` ga o'tkazing (konventsiya).
- **App root** (`app.ts:12-17`): `ChangeDetectionStrategy.OnPush` yo'q — qo'shing.
- **Portal/agent guardlar** (`portal.guard.ts`, `agent-portal.guard.ts`): localStorage'ni to'g'ridan-to'g'ri o'qiydi — service signallarini (`isAuthenticated()`) ishlating.
- **Token store helperi:** `auth.service.ts`, `portal.service.ts`, `agent-portal.service.ts` bir xil token-signal+localStorage patternni takrorlaydi (~90 qator). `createTokenStore(storageKey, loginRoute)` helperi bilan yig'ing — 1.1 kabi driftni oldini oladi.

---

# ISH TARTIBI (tavsiya)

1. **FAZA 1 to'liq** — portalni ishga tushiradi, o'lik funksiyalarni tuzatadi, xavfsizlik/UX sikllarni to'xtatadi. Eng katta ta'sir.
2. **FAZA 2** — noto'g'ri data ko'rsatadigan sahifalar (movements, dashboard, counterparty).
3. **FAZA 3** — sana muammosi (ayniqsa KPI yozuvi — data buzilishi).
4. **FAZA 4** — forma validatsiyasi + toast tozalash.
5. **FAZA 5** — i18n + dark mode (ko'rinish/tarjima).
6. **FAZA 6** — vaqt bo'lsa.

Har fazadan keyin: `ng build` xatosiz o'tsin. Iloji bo'lsa dev server (`ng serve`, port 7050) + backend (port 7040) bilan qo'lda tekshiring.

## Backend'ga qo'shilishi kerak bo'lishi mumkin bo'lgan endpointlar (1.7 va boshqalardan)
Agar bu funksiyalar UI'da qolsa, backend jamosidan so'rang:
- `PUT /auth/profile`, `PUT /auth/change-password` (profil sahifasi — MUHIM)
- `GET /counterparties/{id}` (kontragent detail)
- `PUT`/`DELETE /locations/{id}` (lokatsiya boshqaruvi)
- `PUT`/`DELETE /qc/parameters/{id}` (QC parametr boshqaruvi)
- `DELETE /finance/transactions/{id}` (tranzaksiya o'chirish)
- `/transfers` endpointida `counterpartyId` filtri (agar yo'q bo'lsa)
