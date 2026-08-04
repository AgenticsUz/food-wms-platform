# WMS — Frontend arxitekturasi (`wms-ui` + `wms-admin`)

> **Oxirgi yangilanish:** 2026-08-04 · **Branch:** `saas-admin`
> **Holat:** ikkala ilova prod build **0 xato** (`wms-ui` 785 kB, `wms-admin` 677 kB initial).
> Umumiy loyiha qoidalari va qolgan ishlar: **`CLAUDE.md`** · Backend: **`BACKEND.md`**

---

## 1. Ikki ilova, bitta backend

| Ilova | Kim uchun | Port | API prefiksi | Til | Token kaliti |
|---|---|---|---|---|---|
| **`wms-ui`** | Zavod xodimlari (tenant) | 7050 | `/api/*` | 4 til (Transloco) | `token` |
| **`wms-admin`** | Platforma egasi (SuperAdmin) | 7060 | `/api/admin/*` | faqat inglizcha | `adminToken` |

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
│   ├── services/        # 26 servis (quyida)
│   ├── models/          # 17 model fayli
│   ├── guards/          # auth · module · permission · portal · agent-portal
│   ├── interceptors/    # auth · error
│   └── config/          # apex-defaults.ts
├── layout/
│   ├── shell/           # sidebar + header + banner + router-outlet
│   ├── sidebar/         # dinamik nav (modul + permission bo'yicha)
│   └── header/          # til, mavzu, valyuta kurslari, bildirishnoma
├── shared/
│   ├── components/      # page-header · status-badge · empty-state ·
│   │                    # notification-bell · import-button · phone-input ·
│   │                    # subscription-banner
│   ├── directives/      # has-permission.directive.ts
│   ├── services/        # notification.service.ts (yagona toast nuqtasi)
│   ├── styles/          # module-common.scss
│   └── utils/           # date.util · transfer-enums · delivery-enums
├── modules/             # 14 funksional modul (quyida)
└── assets/i18n/         # uz · uz-cyrl · ru · en  (574 kalit, parite majburiy)
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
| `auth` | login · register (self-service) | — |
| `portal` | kontragent portali: login · dashboard · transfers · finance | alohida token |
| `agent-portal` | agent portali: login · dashboard | alohida token |

### 2.3 Core servislar (26)

`api` (HTTP wrapper) · `auth` · `tenant` (enabledModules) · `permission` · **`subscription`** ·
`product` · `warehouse` · `transfer` · `production` · `counterparty` · `agent` · `delivery` ·
`finance` · `kpi` · `analytics` · `audit` · `settings` · `portal` · `agent-portal` ·
`notification-bell` · `export` · `import` · `currency` · `theme` · `loading` · `transloco-loader`.

### 2.4 Himoya qatlamlari

```
authGuard          → token bormi
moduleGuard(CODE)  → TenantService.enabledModules() ichidami
permissionGuard(c) → PermissionService.can(c)
*hasPermission     → tugmalarni DOM'dan olib tashlaydi (yashirmaydi)
```

Route'larda tartib: `[moduleGuard('X'), permissionGuard('x.view')]`.
**Sidebar** `visibleNavItems` computed'i shu ikki shartni ham qo'llaydi —
yopiq modul menyuda umuman ko'rinmaydi.

> Bu faqat UX qatlami. Haqiqiy cheklov backendda (`BACKEND.md` §4.3).

### 2.5 Obuna (SaaS) qatlami

| Qism | Fayl |
|---|---|
| Model | `core/models/subscription.model.ts` — `SubscriptionInfo`, `LimitUsage`, `SubscriptionPlan` |
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
| 402 | `subscription_suspended` · `trial_expired` · `tenant_inactive` | Tarjima qilingan xabar + `/settings/subscription` ga yo'naltirish |
| 402 | `limit_users` · `limit_warehouses` · `limit_transfers` | Faqat xabar (yo'naltirish yo'q) |
| 403 | `module_disabled:*` | "Bu modul obuna planingizga kirmaydi" |
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
│   ├── services/     # api · auth · platform · notification
│   ├── models/       # api-response · auth · tenant · plan
│   ├── guards/       # auth.guard.ts
│   └── interceptors/ # auth.interceptor.ts (401 → logout, boshqa xato → toast)
├── layout/           # shell (sidebar: Dashboard · Tenants · Plans)
└── modules/
    ├── login/        # faqat SuperAdmin kiradi
    ├── dashboard/    # platforma KPI + tenant o'sishi (ApexCharts) + so'nggi tenantlar
    ├── tenants/      # CRUD · suspend/activate · plan · modullar · trial
    └── plans/        # CRUD · modul to'plami · limitlar · trialDays · isDefault
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
- **Transloco yo'q** — ilova bir tilda; `@jsverse/transloco` dependency olib tashlangan.

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
| `isPlatformAction` ko'rsatilmaydi | Backend `AuditLogDto` da bor; `wms-ui` audit sahifasida badge yo'q, `wms-admin` da audit sahifasi umuman yo'q |
| Limit 80 % ogohlantirishi | Faqat Obuna sahifasidagi progress-bar rangi; yaratish paytida ogohlantirish yo'q |
| Ba'zi toast matnlari qattiq yozilgan | CRUD komponentlarida bir qism success/error matnlari hali inglizcha (i18n qamroviga kirmagan) |
| Plan o'zgartirish oqimi | Faqat "biz bilan bog'laning" — self-service upgrade billing bilan birga keladi |
