# WMS — Frontend arxitekturasi (`wms-ui` + `wms-admin`)

> **Oxirgi yangilanish:** 2026-08-05 · **Branch:** `saas-admin`
> **Holat:** ikkala ilova prod build **0 xato** (`wms-ui` 785 kB, `wms-admin` 763 kB initial).
> Umumiy loyiha qoidalari va qolgan ishlar: **`CLAUDE.md`** · Backend: **`BACKEND.md`**

---

## 1. Ikki ilova, bitta backend

| Ilova | Kim uchun | Port | API prefiksi | Til | Token kaliti |
|---|---|---|---|---|---|
| **`wms-ui`** | Zavod xodimlari (tenant) | 7050 | `/api/*` | 4 til (Transloco) | `token` |
| **`wms-admin`** | Platforma egasi (SuperAdmin) | 7060 | `/api/admin/*` | 3 til (Transloco) | `adminToken` |

Ikkalasi **alohida** Angular loyihasi, alohida `package.json`, alohida deploy
(`wms-admin` → alohida subdomen, masalan `admin.domain.uz`).
Dizayn tizimi umumiy: `styles.scss` nusxalangan — **o'zgartirsangiz ikkalasiga ham qo'llang.**

### Umumiy texnologiya

Angular **21** (standalone, signals, **zoneless**) · PrimeNG **21.1.5** (Aura preset) ·
Tailwind CSS v4 · ApexCharts + ng-apexcharts · PrimeIcons · SCSS.
State — **faqat signals**, NgRx yo'q.

---

## 2. `wms-ui` — tenant ilovasi

### 2.1 Papka tuzilmasi

```
wms-ui/src/app/
├── core/
│   ├── services/        # 28 servis (quyida)
│   ├── models/          # 19 model fayli
│   ├── guards/          # auth · module · feature · permission · portal · agent-portal
│   ├── interceptors/    # auth · language · error
│   └── config/          # apex-defaults.ts
├── layout/
│   ├── shell/           # sidebar + header + banner + router-outlet
│   ├── sidebar/         # dinamik nav (modul + permission bo'yicha)
│   └── header/          # til, mavzu, valyuta kurslari, bildirishnoma
├── shared/
│   ├── components/      # page-header · status-badge · empty-state ·
│   │                    # notification-bell · import-button · phone-input ·
│   │                    # subscription-banner · upgrade-banner
│   ├── directives/      # has-permission.directive.ts
│   ├── services/        # notification.service.ts (yagona toast nuqtasi)
│   ├── styles/          # module-common.scss
│   └── utils/           # date.util · transfer-enums · delivery-enums
├── modules/             # 15 funksional modul (quyida)
└── assets/i18n/         # uz · uz-cyrl · ru · en  (613 kalit, parite majburiy)
```

### 2.2 Modullar va sahifalar

| Modul | Sahifalar | Modul kodi (gate) |
|---|---|---|
| `dashboard` | KPI kartalar + 3 grafik + so'nggi transferlar | — |
| `warehouse` | stock-overview · warehouse-list · locations · batches · movements | `WAREHOUSE_RAW` |
| `production` | stages · recipe-list/create/detail · order-list/create/detail | `PRODUCTION` |
| `transfers` | list · create · detail (tasdiqlash/rad etish/qaytarish) | `TRANSFERS` |
| `finance` | summary · transactions · debts · payments | `FINANCE` |
| `kpi` | dashboard · shifts · plans · actuals · attendance | `KPI` |
| `counterparties` | supplier-list · client-list · detail | `SUPPLIERS`/`CLIENTS` |
| `agents` | agent-list · agent-detail (komissiya) | `AGENTS` |
| `delivery` | deliveries · create · detail · vehicles · drivers | `DELIVERY` |
| `products` | product-list · categories · units | — |
| `settings` | users · roles · modules · **subscription** · qc-parameters · audit-log · profile | — |
| `auth` | login · register — **demo so'rovi** (`request-demo` aliasi bilan) | — |
| `portal` | kontragent portali: login · dashboard · transfers · finance | alohida token |
| `agent-portal` | agent portali: login · dashboard | alohida token |
| `custom` | maxsus (per-mijoz) fitchalar — konvensiya va namuna skelet | `custom.*` feature |

### 2.3 Core servislar (26)

`api` (HTTP wrapper) · `auth` · `tenant` (enabledModules) · `permission` · **`subscription`** ·
**`feature`** (enabledFeatures) · **`lead`** (demo so'rovi) ·
`product` · `warehouse` · `transfer` · `production` · `counterparty` · `agent` · `delivery` ·
`finance` · `kpi` · `analytics` · `audit` · `settings` · `portal` · `agent-portal` ·
`notification-bell` · `export` · `import` · `currency` · `theme` · `loading` · `transloco-loader`.

### 2.4 Himoya qatlamlari

```
authGuard          → token bormi
moduleGuard(CODE)  → TenantService.enabledModules() ichidami
featureGuard(CODE) → FeatureService.isEnabled(CODE) — moduldan mayda dona
permissionGuard(c) → PermissionService.can(c)
*hasPermission     → tugmalarni DOM'dan olib tashlaydi (yashirmaydi)
```

Route'larda tartib: `[moduleGuard('X'), permissionGuard('x.view')]`,
feature darajasida `[featureGuard('finance.debts')]` — hozir **28 joyda** ishlatiladi.

`featureGuard` ro'yxat bo'sh bo'lsa **o'tkazadi** (backend katalogni yubormaguncha menyu
o'zgarmasin), lekin `custom.*` kodlari har doim **yopiq** — maxsus fitcha faqat aniq
yoqilganda ko'rinadi.
**Sidebar** `visibleNavItems` computed'i shu ikki shartni ham qo'llaydi —
yopiq modul menyuda umuman ko'rinmaydi.

> Bu faqat UX qatlami. Haqiqiy cheklov backendda (`BACKEND.md` §4.3).

### 2.5 Obuna (SaaS) qatlami

| Qism | Fayl |
|---|---|
| Model | `core/models/subscription.model.ts` — `SubscriptionInfo` (`paidUntil`, `daysUntilPaidEnd`, `paymentGraceDays`, `blockedReason`, `blockedMessage`, `suspendedUntil`, `enabledModules`, `enabledFeatures`, `limits` obyekt sifatida), `LimitRow`, `SubscriptionPlan` |
| Servis | `core/services/subscription.service.ts` — `info` signal, `load()`, `fetch()`, `plans()`, `clear()` |
| Sahifa | `modules/settings/subscription/` — plan kartasi, trial/grace, limitlar progress-bar, modul chiplari, mavjud planlar jadvali |
| Banner | `shared/components/subscription-banner/` — shell'da, header ostida |
| Aloqa | `environment.supportPhone` / `supportEmail` |

**Yuklanish:** `ShellComponent.ngOnInit` → `subscriptionService.load()`.
Shell faqat autentifikatsiyalangan asosiy ilovada quriladi, ya'ni login'dan keyin ham,
mavjud token bilan ochilganda ham ishlaydi va portal route'lariga tegmaydi.
`AuthService.logout()` → `subscriptionService.clear()`.

**Banner qoidasi:** `isExpiringSoon` → sariq chiziq, kuniga bir marta yopiladi
(`localStorage: subscriptionBannerDismissed=<sana>`); `isBlocked` → qizil chiziq, **yopilmaydi**.

**Modullar sahifasi faqat-ko'rish** — toggle yo'q, `Yoqilgan`/`O'chiq` badge + izoh paneli
va Obuna sahifasiga havola (backend tenant adminning toggle qilishini 403 bilan rad etadi).

### 2.6 Xato boshqaruvi

`error.interceptor.ts` — **matn bo'yicha emas, `err.error.code` bo'yicha** qaror qabul qiladi:

| Status | `code` | Xatti-harakat |
|---|---|---|
| 401 | — | `auth.interceptor` hal qiladi (toast yo'q) |
| 402 | `trial_expired` · `payment_expired` · `suspended_nonpayment` · `suspended_request` · `suspended_technical` · `suspended_violation` · `suspended_other` · `tenant_inactive` | Tarjima qilingan xabar + `/settings/subscription` ga yo'naltirish. Backend `blockedMessage` yuborsa — **o'sha ustun** |
| 402 | `limit_users` · `limit_warehouses` · `limit_transfers` | Faqat xabar (yo'naltirish yo'q) |
| 403 | `module_disabled:*` | "Bu modul obuna planingizga kirmaydi" |
| 403 | `feature_disabled:*` | "Bu imkoniyat tarifingizga kirmaydi" |
| 403 | boshqa | "Ruxsat yo'q" |
| 400/409/422 | — | Backend `message` |
| 404 / 500 / 0 | — | Tarjima qilingan umumiy matn |

**Login sahifasi** 402 ni interceptor'dan mustaqil ko'rsatadi — qizil panel + support
telefon/email (toast bilan yo'qolib ketmaydi).

**Bitta toast qoidasi:** interceptor allaqachon xabar chiqaradi — komponent `error`
callback'ida **ikkinchi toast qo'shilmaydi**, faqat holat tozalanadi (`saving.set(false)`).

### 2.7 Token izolyatsiyasi

`auth.interceptor.ts` tokenni **URL bo'yicha** tanlaydi:
`/agent-portal/` → `agentPortalToken` · `/portal/` → `portalToken` · aks holda asosiy JWT.
401 da **mos** servisning `logout()` i chaqiriladi — portal 401 asosiy sessiyani o'chirmaydi.

---

## 3. `wms-admin` — control plane

### 3.1 Tuzilma

```
wms-admin/src/app/
├── core/
│   ├── services/     # api · auth · platform · notification · theme
│   ├── models/       # api-response · auth · tenant · plan · lead · feature · organization
│   ├── guards/       # auth.guard.ts
│   ├── interceptors/ # auth (401 → logout, boshqa xato → toast) · language (Accept-Language)
│   ├── utils/        # date.util.ts (utcDateOnly, daysUntil)
│   └── transloco-loader.ts
├── layout/           # shell: sidebar (navigatsiya) + topbar (til · mavzu · foydalanuvchi)
├── modules/
│   ├── login/          # faqat SuperAdmin kiradi · til tanlagich shu yerda ham
│   ├── dashboard/      # platforma KPI + o'sish grafigi + so'nggi tenantlar
│   ├── tenants/        # CRUD · to'lov · suspend · modullar · feature'lar · trial
│   ├── leads/          # demo so'rovlari · filtr · drawer · tenantga aylantirish
│   ├── organizations/  # STIR bo'yicha kompaniyalar · bog'lanishlar
│   └── plans/          # CRUD · modul + feature to'plami · limitlar · trialDays · isDefault
└── public/i18n/      # uz · ru · en (266 kalit, parite majburiy)
```

### 3.2 Xususiyatlari

- **Login** — `res.data.user.isSuperAdmin` tekshiriladi; oddiy tenant admini rad etiladi
  ("This console is for platform administrators only").
- **JWT `exp`** startupda client tomonda tekshiriladi — muddati o'tgan token bilan UI
  ochilmaydi (birinchi 401 kutilmaydi).
- **Tenants jadvali** — "Trial ends" ustuni: 3 kundan kam → qizil, 7 dan kam → sariq.
- **Modullar dialogi** — plan to'plamidan tashqarida qo'lda yoqilgan modulga
  **"outside plan"** badge'i.
- **Plans jadvali** — default planda **DEFAULT** badge, Trial ustuni.
  Default plan bittagina bo'ladi (backend kafolatlaydi) va o'chirilmaydi.
- **Uch til** — `uz` (standart), `ru`, `en`. Katalog `public/i18n/` da (`src/assets` emas —
  `angular.json` shu papkani assets sifatida ko'chiradi). Til tanlagich topbar'da va login
  sahifasida; tanlov `localStorage: adminLang` da saqlanadi.
- **Topbar** — sahifa sarlavhasi marshrutdan olinadi, shuning uchun sahifalar o'z `<h1>` ini
  takrorlamaydi. O'ng tomonda: til · mavzu · foydalanuvchi menyusi (chiqish).
- **Dark mode** — `ThemeService` `documentElement` ga `.dark-mode` qo'yadi. Standart holatda
  tizim sozlamasiga ergashadi, foydalanuvchi bosgach `localStorage: adminTheme` da saqlanadi.
- **Sahifa qolipi** — sarlavha ostida bitta izoh qatori, keyin `.page-toolbar` kartasi
  (chapda filtrlar, o'ngda asosiy amal), keyin jadval. `styles.scss` da global uslub.
- **Brend rangi** — `definePreset` orqali Aura'ning primary palitrasi pistachio bilan
  almashtirilgan. `:root` dagi `--p-*` override'lari preset ustidan ishlamaydi.
- **Feature dialogi** — modul bo'yicha akkordeon, uch holatli boshqaruv (Plan / On / Off),
  `override` badge'i, "hammasini tarifga qaytarish", maxsus fitchalar uchun alohida filtr.

---

## 4. Konventsiyalar (ikkala ilova uchun qat'iy)

- `inject()` — konstruktor injeksiyasi **yo'q**.
- Barcha komponentlar `standalone: true` + `ChangeDetectionStrategy.OnPush`.
- Holat — `signal()` / `computed()` / `effect()`. `BehaviorSubject` ishlatilmaydi.
- HTTP — **faqat `ApiService`** orqali, `HttpClient` to'g'ridan-to'g'ri emas.
- Toast — **faqat `NotificationService`**, `MessageService` to'g'ridan-to'g'ri emas.
- `wms-ui` da har matn Transloco orqali va **4 ta faylga ham** qo'shiladi
  (`uz`, `uz-cyrl`, `ru`, `en`) — parite majburiy.
- Sana — backend UTC, foydalanuvchi UTC+5. **`toISOString().split('T')[0]` ishlatmang**
  (bir kun oldinga siljitadi) — `shared/utils/date.util.ts` dagi `toLocalDateString(d)`
  va `parseUtc(s)` ishlatiladi.
- Enum → matn: `shared/utils/transfer-enums.ts` / `delivery-enums.ts`.
  `*Class` → `StatusBadgeComponent` uchun CSS kaliti, `*Key` → Transloco kaliti.
- **Gotcha:** PrimeNG `<ng-template #body let-t>` dagi `let-t` Transloco'ning `t`
  funksiyasini **soyalaydi**. Body ichida tarjima kerak bo'lsa `let-row` deb nomlang.
- Dark mode — komponent SCSS'ida `:host-context(.dark-mode)`.
- Grafiklar — `APEX_DEFAULTS` ni spread qilib, keyin override.
- Har o'zgarishdan keyin: `ng build --configuration production` → **0 xato**.

---

## 5. Dizayn tizimi

`styles.scss` dagi CSS o'zgaruvchilari (ikkala ilovada bir xil):

```
Palitra:  pistachio (primary) · berry (danger) · caramel (warning) ·
          cocoa (neutral) · blueberry (info) · cream (fon)
Radius:   --radius-sm 8px · md 12px · lg 16px · xl 20px
Soya:     --shadow-sm / md / lg
Matn:     --text-primary / secondary / muted
Fon:      --bg-base / surface / muted · --border
```

Tayyor klasslar: `.wms-card` · `.pill` + `.pill-{success,warning,danger,info,neutral}` ·
`.badge-{success,warning,danger,info,neutral}` · `.page-enter` · `.skeleton` ·
`.text-right` · `.item-name` · `.empty-state`.

Shrift: **DM Sans** (matn) + **JetBrains Mono** (raqam, kod — `.amount`, `.num`).
Layout: sidebar 260px (yig'ilganda 72px), header 64px, kontent padding 24/16/12px.

---

## 6. Ishga tushirish

```bash
# Tenant ilova
cd wms-ui && npm install && ng serve            # http://localhost:7050

# Control plane
cd wms-admin && npm install && ng serve         # http://localhost:7060

# Prod build (ikkalasi ham 0 xato bo'lishi shart)
ng build --configuration production
```

Deploy: `dist/<app>/browser` → Nginx static; `/api` → backendga proxy.
`environment.prod.ts` da `apiUrl: '/api'` (nisbiy — HTTPS'da mixed-content bo'lmasin).

---

## 7. Ma'lum bo'lgan kamchiliklar (frontend)

| Nima | Izoh |
|---|---|
| `supportPhone` / `supportEmail` — **placeholder** | `environment*.ts` da. Bloklangan mijoz aynan shuni ko'radi — deploydan oldin haqiqiysiga almashtirilsin |
| Limit 80 % ogohlantirishi | Faqat Obuna sahifasidagi progress-bar rangi; yaratish paytida ogohlantirish yo'q |
| `wms-ui` da ba'zi toast matnlari qattiq yozilgan | ~166 noyob matn; ularning 79 tasi `error.interceptor` ustiga **ikkinchi toast** chiqaradi (bitta-toast qoidasi buzilgan) — tarjima emas, olib tashlash kerak |
| Brendlash (logo, rang) | Backend `Branding` maydonlari hali yo'q — UI ham yo'q |
| Limit 80 % ogohlantirishi | Backend `usagePercent` / `warning` yubormaydi |
| `wms-admin` da audit sahifasi | Backendda `/api/admin/audit` yo'q; `/api/audit` faqat o'z tenanti bilan chegaralangan |
| Plan o'zgartirish oqimi | Faqat "biz bilan bog'laning" — self-service upgrade billing bilan birga keladi |
