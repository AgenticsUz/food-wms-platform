# WMS — Frontend arxitekturasi (`wms-ui` + `wms-admin`)

> **Oxirgi yangilanish:** 2026-08-06 · **Branch:** `saas-admin`
> **Holat:** ikkala ilova prod build **0 xato**.
> **2026-08-06 jonli sinovda topilgan tuzatishlar:** tenant slug'ini ish vaqtida aniqlash
> (§2.8 — ilgari faqat tizim tenanti kira olardi), har marshrutga `permissionGuard` va
> `**` wildcard (§2.4), `dark:` variantini ilova mavzu klassiga bog'lash (§5), aloqa
> ma'lumotini serverdan olish (§2.8), 12 komponentdagi ikkilangan toastni olib tashlash (§2.6).
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
│   ├── services/        # 29 servis (quyida)
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
└── assets/i18n/         # uz · uz-cyrl · ru · en  (634 kalit, parite majburiy)
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

### 2.3 Core servislar (30)

`api` (HTTP wrapper) · `auth` · `tenant` (enabledModules) · `permission` · **`subscription`** ·
**`feature`** (enabledFeatures) · **`lead`** (demo so'rovi) ·
`product` · `warehouse` · `transfer` · `production` · `counterparty` · `agent` · `delivery` ·
`finance` · `kpi` · `analytics` · `audit` · `settings` · `portal` · `agent-portal` ·
`notification-bell` · `export` · `import` · `currency` · `theme` · `loading` · `transloco-loader` ·
**`public-info`** (tokensiz sahifalar uchun aloqa ma'lumoti) ·
**`branding`** (mijoz logotipi va rangi — §2.9).

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

> **Har ikkalasi ham qo'yilishi shart.** 2026-08-06 gacha `production`, `warehouse`,
> `transfers`, `counterparties`, `products` va butun `settings` bo'limida faqat modul
> tekshirilardi (`finance`/`kpi` da esa ikkalasi ham bor edi). Backend baribir 403
> qaytarardi, lekin foydalanuvchi manzilni qo'lda yozsa sahifaga kirib, keyin xato
> toastiga urilardi. `settings` ning standart bo'limi ham `users` dan `profile` ga
> ko'chirildi — uni har qanday xodim ocha oladi.
>
> Noma'lum manzil uchun `{ path: '**', redirectTo: 'dashboard' }` bor; ilgari wildcard
> yo'q edi va router outlet bo'sh qolib, oq sahifa ko'rinardi.

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
| Aloqa | **Serverdan:** obuna sahifasi `/api/subscription/me` dan, login va demo sahifalari `core/services/public-info.service.ts` orqali `/api/public/branding` dan. `environment.supportPhone` / `supportEmail` — faqat zaxira |

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

### 2.8 Qaysi tenantga kirilyapti (`tenant-slug.util.ts`)

`POST /api/auth/login` `tenantSlug` ni **majburiy** so'raydi: telefon raqam global emas,
faqat tenant ichida noyob. Qiymat ish vaqtida shu tartibda aniqlanadi:

1. **Subdomen** — `sinov.wms.uz` → `sinov`. `www`/`app`/`admin`/`api`, `localhost` va IP
   manzillar tenant deb qaralmaydi.
2. Oxirgi muvaffaqiyatli kirishda eslab qolingan qiymat (`localStorage: tenantSlug`).
3. `environment.tenantSlug` — standart.

Subdomen slug bersa login formasida maydon **ko'rsatilmaydi**; aks holda (localhost,
yalang'och domen) "Tashkilot kodi" so'raladi va kirish muvaffaqiyatli bo'lgach eslab
qolinadi.

> Ilgari bu qiymat `environment.tenantSlug` da qattiq `'admin'` yozilgan edi — ya'ni
> bitta build faqat bitta mijozga xizmat qilardi va admin panelda yaratilgan yangi
> tenantning hisobi ishlasa ham, unga kirishning **iloji yo'q edi**. R20 (subdomen)
> shu qatlam ustida qo'shimcha ishsiz ishlaydi.

### 2.9 Mijoz brendi (`branding.service.ts`)

Brendlash **ma'lumot**, kod emas: bitta build hamma mijozga xizmat qiladi. Serverdan uch
maydon keladi — `logoUrl`, `logoSquareUrl`, `brandColor` — va aynan shu obyekt uchta
manbada bir xil: login javobi, `GET /api/subscription/me`, `GET /api/public/branding?slug=`.

**Rang.** Mijozdan **bitta** rang so'raladi; qolgan palitra shundan hosil qilinadi (oq/qora
bilan aralashtirish). Qiymatlar `document.documentElement` ning inline uslubiga yoziladi,
shuning uchun `styles.scss` dagi `:root` qoidasidan ustun turadi:

| O'zgaruvchi | Kim ishlatadi |
|---|---|
| `--p-primary-50…700`, `--p-primary-color`, `--p-button-primary-*` | PrimeNG Aura |
| `--brand-primary`, `--brand-primary-soft`, `--brand-primary-strong` | ilova uslublari (sidebar aksentlari) |

Ilova uslublarida har doim zaxira bilan yoziladi — `var(--brand-primary, var(--color-pistachio-500))`.
`brandColor` `null` bo'lsa o'zgaruvchilar **o'chiriladi** va standart pistachio palitrasi qaytadi.
Dark mode buzilmaydi: faqat primary ranglar almashtiriladi, sirt (surface) ranglariga tegilmaydi.

**Logo va sarlavha.**

| Joy | Manba |
|---|---|
| Sidebar (yoyilgan) | `logoUrl` → tenant nomi matn sifatida |
| Sidebar (yig'ilgan, 72px) | `logoSquareUrl` → `logoUrl` → standart belgi |
| Favicon | `logoSquareUrl` → `logoUrl` → `favicon.ico` |
| Tab sarlavhasi | `«Tenant nomi» — WMS` → `WMS Platform` |

**Miltillashning oldi.** Rang CSS o'zgaruvchilari bilan qo'llanadi, ya'ni faqat Angular
ishga tushgandan keyin ta'sir qiladi. Serverdan javob kutilsa mijoz avval bizning yashil
rangimizni, so'ng o'zinikini ko'rardi. Shuning uchun oxirgi ma'lum brend `localStorage`
(`branding`) da turadi va `provideAppInitializer` da — birinchi chizishdan **oldin** —
qo'llanadi. Faqat token mavjud bo'lganda: **login sahifasi doim standart** ko'rinishda
qoladi, chunki bitta URL'da kim kirayotgani hali noma'lum.

**Logout'da tozalanadi.** Bitta kompyuterdan ikki mijoz kirsa, ikkinchisiga birinchisining
logotipi va rangi ko'rsatilishi shunchaki chiroyli emas — bu boshqa kompaniyaning brendi.

> `GET /api/public/branding?slug=` allaqachon tayyor (`public-info.service` uni aloqa
> ma'lumoti uchun chaqiradi). Har mijozga subdomen berilganda (R20) login sahifasini
> brendlash uchun shu javobning `branding` qismini qo'llash yetarli.

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
│   ├── tenants/        # CRUD · to'lov · suspend · modullar · feature'lar · trial · parol tiklash
│   ├── leads/          # demo so'rovlari · filtr · drawer · tenantga aylantirish
│   ├── organizations/  # STIR bo'yicha kompaniyalar · bog'lanishlar
│   └── plans/          # CRUD · modul + feature to'plami · limitlar · trialDays · isDefault
└── public/i18n/      # uz · ru · en (315 kalit, parite majburiy)
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
- **Parolni tiklash** (`GET/POST admin/tenants/{id}/users` · `.../reset-user-password`) —
  mijoz o'z tizimidan qulflanib qolganda telefon orqali qaytarish yo'li. Dialog ochilganda
  **admin rolidagi** foydalanuvchi oldindan tanlangan (backend `userId` bo'lmasa aynan shuni
  topadi); faolsiz hisoblar ro'yxatda qoladi, lekin **Nofaol** yorlig'i bilan. Parol
  avtomatik yaratiladi yoki qo'lda kiritiladi (**min 8 belgi** — backenddagi
  `PasswordGenerator.MinimumManualLength` bilan bir xil).
  > **Natija ekrani ikkinchi, alohida `p-dialog`.** Sabab — PrimeNG `closable` /
  > `closeOnEscape` tinglovchilarini dialog **ochilganda bir marta** bog'laydi va keyin bu
  > inputlar o'zgarsa qayta ko'rib chiqmaydi. Bitta dialogda natija kelgach bayroqlarni
  > `false` ga o'tkazish yetarli emas edi: Esc baribir yopib, qaytarib bo'lmaydigan parolni
  > yo'qotardi. Shuning uchun natija dialogi boshidanoq `closable=false`,
  > `closeOnEscape=false`, `dismissableMask=false` bilan yaratiladi va faqat **Done** bilan
  > yopiladi.
  >
  > Parol javobda **bir marta** keladi: u toastga ham, konsolga ham chiqarilmaydi (toastda
  > faqat "Nusxalandi"). Nusxalash — Clipboard API, oddiy `http` orqali ochilgan konsol uchun
  > `execCommand` zaxirasi bilan.
- **Brendlash** (tenant formasidagi **BRENDLASH** bo'limi) — ikki logo slot (keng va
  kvadrat) darhol ko'rinish bilan, `<input type="color">` + hex maydon, rangni tozalash
  tugmasi va **jonli ko'rinish**: mijozning sidebar'i va tugmasi tanlangan rangda.
  Rang ustidagi oq matn kontrasti WCAG AA (4.5:1) dan past bo'lsa ogohlantiriladi, lekin
  **bloklanmaydi** — aks holda mijoz "sizning tizimingiz mening rangimni qabul qilmaydi"
  deydi. Fayl turi va hajmi (512 KB) klientda ham tekshiriladi: backend baribir rad etadi,
  lekin foydalanuvchi 512 KB'ni yuklab bo'lib eshitmasligi kerak. Yuklash `FormData` bilan
  to'g'ridan-to'g'ri `HttpClient` orqali ketadi — `ApiService` `Content-Type` ni o'zi
  qo'yadi va bu `multipart` chegarasini buzardi. Ro'yxatdagi mijoz nomi yonida ham kvadrat
  logo ko'rsatiladi.

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

### Qora rejim va Tailwind `dark:`

Mavzu `documentElement` dagi **`.dark-mode`** klassi bilan boshqariladi (`ThemeService`),
PrimeNG esa `darkModeSelector` orqali shunga ulanadi.

Tailwind v4 da `dark:` **standart holatda operatsion tizimning `prefers-color-scheme`
sozlamasiga** bog'lanadi. Ikkovi bog'lanmagani uchun OS qora rejimda bo'lsa ilova yorug'
mavzuni chizardi-yu, `dark:text-white` kabi utilitalar yonib turardi — natijada umumiy
`page-header` komponenti sabab **har bir sahifa sarlavhasi oq fonda oq** bo'lib ko'rinmasdi.

Ikkala `styles.scss` da endi:

```scss
@custom-variant dark (&:where(.dark-mode, .dark-mode *, .dark, .dark *));
```

> Yangi `dark:` utilitasi qo'shsangiz — shu qatorsiz u OS sozlamasiga ergashadi.
> Eslatma: `wms-admin` `ThemeService` i standart holatda OS sozlamasiga ergashadi,
> `wms-ui` esa har doim yorug'dan boshlaydi (saqlangan tanlov bo'lmasa).

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

Deploy: `dist/<app>/browser` → Nginx static; `/api` **va** `/uploads` → backendga proxy.
To'liq nginx qolipi va domenlar: `CLAUDE.md` §6.

**Ikkala ilovada ham `apiUrl: '/api'` — nisbiy.** Sabab:

- so'rov o'z domeniga ketadi → **CORS ishga tushmaydi** (same-origin);
- HTTPS'ga o'tilganda protokol avtomatik mos keladi → mixed-content yo'q va
  frontendni qayta yig'ish shart emas.

> `/uploads` ni unutmang: tenant logolari `/uploads/tenants/...` da, `/api` **ostida emas**.
> 2026-08-08 gacha `wms-admin` absolyut `http://api.warehouse-system.u/api` ishlatardi —
> domende `z` harfi ham tushib qolgan edi, ya'ni prod'da umuman ishlamasdi.

---

## 7. Ma'lum bo'lgan kamchiliklar (frontend)

> Ro'yxat 2026-08-10 da F8–F12 yopilgach qayta o'lchandi. Bajarilganlari olib
> tashlandi (brendlash → §2.9, audit sahifasi → §3.2, limit ogohlantirishi va parol
> tiklash → §2.5 / §3.2); qolganlari `CLAUDE.md` §4 dagi R-raqamlari bilan bog'landi.

| Nima | Izoh |
|---|---|
| **Qattiq yozilgan inglizcha matnlar** (R10) | O'lchangan: `notify.*` da **158**, tasdiq dialoglarida **22**, `placeholder` larda **42** — jami ~222 matn × 4 til. Ichida validatsiya ogohlantirishlari (`Recipe name is required`), enum yorliqlari (`Raw Material`, `No expiry`, `Root`) va ruxsat nomlari (`Manage Warehouse`) bor. PrimeNG `confirmDialog` ning `No`/`Yes` tugmalari ham tarjima qilinmagan. **Ikkilangan** toastlar (12 komponent, 20 joy) allaqachon olib tashlandi |
| Aloqa raqami placeholder (R1 / F7) | Kod tayyor — qiymat serverdagi `Support:Phone` / `Support:Email` dan keladi va frontendni qayta yig'ish shart emas. `environment*.ts` dagi zaxira hali `+998 90 000 00 00`; haqiqiy raqam berilgach almashtiriladi |
| Login sahifasi brendlanmaydi | Ataylab: bitta URL'da kim kirayotgani noma'lum. `GET /api/public/branding?slug=` tayyor, subdomenga o'tilganda (R20) yoqiladi — §2.9 |
| Plan o'zgartirish oqimi (R9) | Faqat "biz bilan bog'laning" — self-service upgrade billing bilan birga keladi |
| `wms-ui` mavzusi OS sozlamasiga ergashmaydi | `wms-admin` ergashadi. Nomuvofiqlik, xato emas — §5 ga qarang |
