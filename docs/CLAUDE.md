# CLAUDE.md — WMS Platform

> **Oxirgi yangilanish:** 2026-08-10 · **Branch:** `saas-admin`
> **Bu fayl — loyihaning kirish nuqtasi va yagona manbasi.**
> Texnik tafsilotlar ikki qo'shni hujjatda:
> **[`BACKEND.md`](BACKEND.md)** (`wms-api`) · **[`FRONTEND.md`](FRONTEND.md)** (`wms-ui` + `wms-admin`).
> Bir reestr: **[`CUSTOM_FEATURES.md`](CUSTOM_FEATURES.md)**. Boshqa hujjat yo'q —
> vazifa va reja fayllari yopilgach o'chiriladi (§8).

---

## 1. Mahsulot

Oziq-ovqat ishlab chiqaruvchilar (muzqaymoq, sut, qandolat, shokolad) uchun
**modular multi-tenant SaaS ombor boshqaruv platformasi**.
Har tenant (zavod) o'z planiga qarab modullarni oladi.

**Asosiy prinsiplar**

- Multi-tenancy — deyarli har jadvalda `TenantId`, izolyatsiya JWT orqali
- Modulli — har imkoniyat yoqiladigan/o'chiriladigan modul
- RBAC — rol va permission darajasida
- Kichik zavoddan yirikigacha miqyoslanadi
- SQLite (MVP) → PostgreSQL-ready

---

## 2. Uchta ilova, bitta backend

| Ilova | Kim uchun | Port | Qamrov |
|---|---|---|---|
| **`wms-ui`** | Zavod xodimlari (tenant) | 7050 | Operatsion WMS — ombor, ishlab chiqarish, moliya, KPI, delivery |
| **`wms-admin`** | Platforma egasi (SuperAdmin) | 7060 | Control plane — tenantlar, planlar, statistika |
| **`wms-api`** | — | 7040 | Umumiy backend: `/api/*` (tenant), `/api/admin/*` (platforma) |

```
wms/
├── docs/          # CLAUDE.md · BACKEND.md · FRONTEND.md  ← faqat shu 3 ta
├── wms-api/       # .NET 8 · Clean Architecture (Domain/Application/Infrastructure/API)
├── wms-ui/        # Angular 21 · tenant ilovasi · 4 til
└── wms-admin/     # Angular 21 · SuperAdmin konsoli · uz · ru · en
```

### Texnologiya

| Qatlam | Texnologiya |
|---|---|
| Backend | .NET 8 LTS, ASP.NET Core Web API, EF Core 8, SQLite, JWT, Serilog |
| Frontend | Angular 21 (standalone, signals, **zoneless**), PrimeNG 21.1.5 (Aura), Tailwind v4 |
| Grafik | ApexCharts + ng-apexcharts |
| i18n | Transloco — `wms-ui`: `uz`, `uz-cyrl`, `ru`, `en` (634 kalit) · `wms-admin`: `uz`, `ru`, `en` (315 kalit, `public/i18n/`) · backend: `uz`, `ru`, `en` |
| IDE | Rider (backend), WebStorm (frontend) |

---

## 3. Hozirgi holat (2026-08-10)

**Cheklovlar API darajasida majburlanadi va mijozga tushunarli ko'rsatiladi.**
Modul gating backendda, obuna har so'rovda tekshiriladi, trial haqiqiy muddat bilan
ishlaydi, plan limitlari amalda; mijoz o'z obunasini va bloklanish sababini ko'radi.

**Biznes modeli (2026-08-04 da o'zgardi):** mijoz bilan gaplashamiz → demo → to'laydi →
**biz** admin paneldan tenant yaratamiz. Public self-service registratsiya **yopiq**;
uning o'rnida demo so'rovi (lead) oqimi. To'lov qo'lda qabul qilinadi va qayd etiladi;
muddati tugasa tizim o'zi to'xtatadi.

| Tomon | Build | Bajarilgan |
|---|---|---|
| `wms-api` | 0 xato, 0 ogohlantirish · **avtomatlashtirilgan sinov yo'q** (solutionda test loyihasi mavjud emas — R23) | SaaS majburlash (modul gate, obuna middleware, trial+grace, limitlar, unique indeks, planlar seed, audit izi) **+ manual billing, muddatli suspend, lead oqimi, feature qatlami, Organization, brendlash, limit ogohlantirishi** |
| `wms-ui` | prod build 0 xato | F1–F12 bajarilgan: obuna sahifasi + banner, modullar faqat-ko'rish, 402/403 kod bo'yicha xato boshqaruvi, feature guardlari, demo so'rovi formasi, portal banneri, parol tiklash + `mustChangePassword`, mijoz brendi, markazlashgan `warning` |
| `wms-admin` | prod build 0 xato | F1–F12 bajarilgan: to'lovlar, sabab bilan suspend, lead ro'yxati + convert, feature matritsasi, organizations, uch tilli interfeys, parol tiklash, brendlash formasi, audit sahifasi |
| i18n (frontend) | `wms-ui` 4 fayl × 634 kalit · `wms-admin` 3 fayl × 315 kalit, farq yo'q | — |
| i18n (backend) | uz / ru / en · 150+ xabar | Xato va tasdiq matnlari `Accept-Language` bo'yicha qaytadi; `code` tarjima qilinmaydi |

**Backend S1–S7 (yangi bosqich, 2026-08-04):**

| # | Ish | Natija |
|---|---|---|
| S1 | To'langan muddat | `Tenant.PaidUntil` + `PaymentRecord`; muddati o'tsa **402 `payment_expired`**; to'lov qayd etilsa tenant o'ziga keladi; `GET /api/admin/tenants/expiring` |
| S2 | Muddatli to'xtatish | Sabab (`NonPayment`/`ClientRequest`/`Technical`/`Violation`/`Other`), ichki izoh, **mijozga ko'rinadigan matn**, `SuspendedUntil` → belgilangan sanada avtomat yoqilish |
| S3 | Registratsiya yopildi | `Registration:SelfServiceEnabled=false` → register **404**; `POST /api/leads` (5/soat/IP, 24 soat dublikat oynasi); admin lead CRUD + `convert` |
| S4 | Feature qatlami | 27 ta feature katalogi, `Plan.FeatureCodes`, `TenantFeature` override, `RequireFeature` → **403 `feature_disabled:CODE`**; yechim: override → plan → default, modul veto bilan |
| S5 | Custom konvensiyasi | `custom.` prefiksi, `IsCustom` → `DefaultEnabled=false`, egasi majburiy, planga qo'shib bo'lmaydi; skelet + `CUSTOM_FEATURES.md` |
| S6 | Organization | INN (9 raqam) bo'yicha platforma darajasidagi kompaniya; counterparty/tenant/import bitta matcher orqali; tenantlar uchun endpoint **yo'q** |
| S7 | Portal → lead | `POST /api/portal/upgrade-interest` va agent portali; `ReferrerTenantId` bilan |

> Muhim: S4 seed'i xatti-harakatni **o'zgartirmaydi** — barcha feature'lar `DefaultEnabled=true`
> va planlarga o'z modullariga qarab to'ldirildi (haqiqiy bazada tekshirilgan: tizim tenanti 29/29).

**Backend B1–B3 (2026-08-05):**

| # | Ish | Natija |
|---|---|---|
| B1 | Brendlash | `Tenant.LogoUrl` / `LogoSquareUrl` / `BrandColor`; `POST/DELETE /api/admin/tenants/{id}/logo?type=wide\|square` (512 KB, SVG/PNG/WebP, o'lcham chegarasi, SVG skript tekshiruvi, fayl nomida hash → cache buzilmaydi); bir xil `branding` obyekti login javobida, `/api/subscription/me` da va `GET /api/public/branding?slug=` da; PDF va Excel sarlavhalarida logo + nom |
| B2 | Limit ogohlantirishi | `ApiResponse.warning` (`{ code, message }`) — **xato emas**, 200 qoladi; `limit_warn_users` / `limit_warn_warehouses` / `limit_warn_transfers`; `/subscription/me` limitlarida `usagePercent` + `isNearLimit`. **Backendda** hisoblanadi; frontend hali o'zi ham hisoblaydi — R7 |
| B3 | Hujjatlar | Shu bo'lim va `BACKEND.md` kod bilan solishtirib yangilandi |
| — | **Parol tiklash** | `GET /api/admin/tenants/{id}/users` · `POST .../reset-user-password` (platforma) va `POST /api/users/{id}/reset-password` (tenant admin). Parol javobda **bir marta**, hech qayerda ochiq saqlanmaydi va audit jurnaliga tushmaydi; 12 belgi, chalkash belgilarsiz; `SecurityStamp` aylanadi → eski tokenlar darhol o'ladi. `wms-admin` UI bajarilgan, `wms-ui` tomoni — F8 |

**Backend B4–B7 (2026-08-10):**

| # | Ish | Natija |
|---|---|---|
| B4 | Platforma audit endpointi | `GET /api/admin/audit` (SuperAdmin) — barcha tenantlar; filtrlar `tenantId`/`userId`/`entityType`/`action`/`platformOnly`/`from`/`to`; sahifalash majburiy (default 50, **max 200**, oshig'i qisqartiriladi, xato emas); javobda `tenantName` + `actorTenantName`; `AuditLog.CreatedAt` ga alohida indeks |
| B5 | Permission katalogi | Har `PermissionDto` da **`isAvailable`** — ruxsat tarifda ishlaydimi. Ro'yxat **kesilmaydi**. `Permission.Module` ↔ `ModuleCodes` mos emas → `PermissionModules` mapping jadvali (`WAREHOUSE`→2 modul, `PARTNERS`→2 modul, `DASHBOARD`/`PRODUCTS`/`SETTINGS`→modulsiz, doim mavjud). Tarifdan tashqari ruxsat **saqlanadi** + `warning: permissions_outside_plan` |
| B6 | `MustChangePassword` | Parolni boshqa odam qo'ygan bo'lsa `true` (ikkala tiklash yo'li, provisioner, xodim yaratish); faqat `ChangePasswordAsync` tozalaydi. Login javobida qaytadi. Backend **bloklamaydi** — majburlash frontendda (F8). Mavjud foydalanuvchilarda `false` |
| B7 | Hujjat ziddiyatlari | Shu fayldagi 4 ta ichki qarama-qarshilik tuzatildi |

**Frontend F8–F12 (2026-08-10):**

| # | Ish | Natija |
|---|---|---|
| F8 | Parol tiklash + `mustChangePassword` | `wms-ui` da tenant admini xodim parolini tiklaydi (natija dialogi **alohida** `p-dialog`, Esc bilan yopilmaydi, parol toastga chiqmaydi); `mustChangePasswordGuard` shell'ga `canActivateChild` — faqat `/settings/profile` o'tadi, `logout` ishlashda qoladi |
| F9 | Brendlash | `wms-admin`: tenant formasida **BRENDLASH** bo'limi — ikki logo, colorpicker + hex, WCAG kontrast ogohlantirishi (bloklamaydi), jonli ko'rinish; ro'yxatda kvadrat logo. `wms-ui`: `branding.service.ts` bitta rangdan palitra hosil qiladi → `--brand-primary` + PrimeNG Aura primary; logo sidebar (yoyilgan/yig'ilgan) va favicon; tab sarlavhasi tenant nomi; `localStorage` + `provideAppInitializer` bilan miltillash yo'q; **logout'da tozalanadi**. Login sahifasi standart qoladi |
| F10 | `wms-admin` audit sahifasi | `GET /api/admin/audit` ustida server tomonda sahifalanadigan `p-table` (lazy); filtrlar: tenant · obyekt turi · sana oralig'i · faqat platforma amallari; amal turlari rangli; tenant qatoridan filtr qo'yilgan havola |
| F11 | Limit ogohlantirishi | Foiz endi **backenddan** (`usagePercent`/`isNearLimit`) — mahalliy hisob olib tashlandi; `ApiService` javobdagi `warning` ni bitta markazlashgan joyda `notify.warn()` ga beradi; yaratish formalarida `limit-notice` paneli (bloklamaydi); Rollar sahifasida tarifga kirmaydigan ruxsat **yashirilmaydi** — kulrang + izoh |
| F12 | `expiring` endpointi | Dashboard mahalliy filtr o'rniga `GET /api/admin/tenants/expiring` dan foydalanadi |
| F7 | Aloqa ma'lumoti | **Ochiq.** Kod tayyor — qiymat serverdagi `Support:Phone` / `Support:Email` dan keladi (R1); `environment*.ts` dagi placeholder faqat zaxira va haqiqiy raqam berilgach almashtiriladi |

**R2 — to'liq zanjir sinovi (2026-08-10):** demo so'rovi → lead → mijozga aylantirish →
brendlash → kirish → plan → feature → limit → to'lov → to'xtatish → qayta yoqilish →
parol → audit. Har qadam **ikkala** ilovada tekshirildi. Natija: zanjir uzilmadi,
uchta nuqson topildi va tuzatildi.

| # | Nuqson | Tuzatish |
|---|---|---|
| 1 | Bosh sahifa plani Moliya/Ishlab chiqarishni o'z ichiga olmagan mijozga ham o'sha kartalarni ko'rsatib, so'rov yuborardi → mijoz kirishi bilan qizil **"Bu modul obuna planingizga kirmaydi"** xatosini ko'rardi | Karta ham, so'rov ham `TenantService.isModuleEnabled` ga bog'landi |
| 2 | `limit-notice` paneli hech qachon chiqmasdi: `subscription/me` sahifa ochilganda bir marta yuklanardi, ya'ni ombor yaratilgach hisob eskirib qolardi (3/3 bo'lsa ham "2/3") | `ApiService` `limit_warn_*` ogohlantirishini ko'rsa obunani qayta o'qiydi (aylanma bog'liqliksiz — `Injector` orqali kech) |
| 3 | Muddatli to'xtatish sanasi kelganda mijoz **24 soatgacha** bloklangan qolardi: qayta yoqish faqat sutkalik fon xizmatida edi, admin esa mijozga "shu sanada qayta yoqiladi" deb va'da qilgan | `SubscriptionPolicy` muddati kelgan to'xtatishni bloklamaydi. To'lov muddati o'tgan bo'lsa baribir bloklanadi (`payment_expired`) — "2 oyga to'xtating" degan mijoz to'lamasdan qaytib qolmasin |

> Kuzatuv (nuqson emas): lead'ni mijozga aylantirish audit yozuvi **platforma** tenantida
> qoladi, chunki amal boshlanganda yangi tenant hali mavjud emas va obyekt — lead.
> Yangi mijozning jurnalidan "qaysi so'rovdan kelgan" ko'rinmaydi.

**Jonli uchma-uch sinov va tuzatishlar (2026-08-06):** uchala ilova birga ishga tushirilib,
yangi tenant mijoz yo'lidan o'tkazildi. Topilgan va tuzatilgan nuqsonlar:

| # | Nuqson | Tuzatish |
|---|---|---|
| 1 | `wms-ui` `tenantSlug` ni qattiq `'admin'` deb yuborardi → **tizim tenantidan boshqa hech kim kira olmasdi** | `tenant-slug.util.ts`: subdomen → eslab qolingan → standart (`FRONTEND.md` §2.8) |
| 2 | Transfer yaratish **har doim** qulardi (`PlanLimits` da oy boshi LINQ ichida hisoblanardi) va yozuv saqlanib qolgani uchun mijoz qayta urinib **dublikat** yaratardi | Hisob so'rovdan tashqariga; ogohlantirish endi hech qachon o'zi hisobot beradigan amaliyotni yiqitmaydi |
| 3 | Yangi tenantda **o'lchov birliklari yo'q** edi (faqat tizim tenantiga seed qilinardi) | `TenantProvisioner` 6 ta standart birlik yaratadi |
| 4 | Mavjud bo'lmagan tenant modullari 404 o'rniga **200** qaytarardi | `TenantService` da tenant borligi tekshiriladi |
| 5 | OS qora rejimda **har bir sahifa sarlavhasi ko'rinmasdi** | `@custom-variant dark` — `dark:` endi ilova mavzu klassiga ergashadi (ikkala ilovada) |
| 6 | Marshrut himoyasi nomuvofiq: `production`/`warehouse`/`transfers`/`counterparties`/`products`/`settings` da faqat modul tekshirilardi | Hammasiga `permissionGuard`; `settings` standarti `profile`; noma'lum manzil uchun `**` → dashboard |
| 7 | Aloqa raqami Angular bundle ichida edi → almashtirish uchun qayta build kerak edi | `GET /api/public/branding` ham `supportPhone`/`supportEmail` qaytaradi; `environment` faqat zaxira |

Tekshirilgan boshqaruv zanjirlari (admin → wms): tenant yaratish → kirish · plan biriktirish →
modullar · modul/feature toggle → 403 va menyu · suspend/activate → 402 · to'lov → `paidUntil` ·
lead → tenant · parol tiklash · plan limitlari. Sahifa sweep: **31 sahifa, 0 konsol xatosi**.

**Asosiy kelishuv:** plani **bor** tenant → plan modullari va limitlari;
plani **yo'q** tenant → **cheksiz** (mavjud mijozlar ishi to'satdan to'xtamasin).

### Nima allaqachon tayyor (mahsulot bo'yicha)

Core WMS (ombor, partiya/LOT + FEFO, transfer, ishlab chiqarish retsept/buyurtma/bosqich,
moliya + qarz, KPI + smena + davomat, sifat nazorati, mahsulot/kontragent) ·
Agent moduli (komissiya) · Delivery moduli (transport, haydovchi, marshrut, yuk xati PDF) ·
Qaytarish (return) · Audit jurnali · Bildirishnomalar + Telegram (config-gated) ·
Excel import/export · PDF · Barcode · Analitika (11 endpoint) · Kontragent va agent portallari ·
Demo so'rovi (lead) oqimi · Kunlik DB backup · `/health` + Serilog · CI/CD · Performance indekslar.

> Public self-service registratsiya **yopiq** (S3) — `Registration:SelfServiceEnabled=false`,
> `register` 404 qaytaradi. Kod olib tashlanmadi: lead'ni tenantga aylantirish aynan
> o'sha provizatsiya yo'lidan foydalanadi.

---

## 4. Qolgan ishlar

### 🔴 Sotuvga chiqishdan oldin majburiy

| # | Kim | Ish |
|---|---|---|
| R1 | DevOps | **Haqiqiy aloqa ma'lumoti.** 2026-08-06 dan boshlab u faqat **serverda** turadi: `appsettings.Production.json` → `Support:Phone` / `Support:Email` (yoki `Support__Phone` env). Login sahifasi ham `GET /api/public/branding` orqali shuni oladi, ya'ni **frontendni qayta yig'ish shart emas**. `environment*.ts` dagi qiymatlar faqat server javob bermaganda ishlaydigan zaxira. Hozir ikkala joyda ham placeholder — bloklangan mijoz aynan shuni ko'radi |
| R2 | Ikkalasi | **Jonli muhitda uchma-uch sinov** — §5 dagi 6 ssenariy. Zanjirning o'zi **lokalda to'liq o'tkazildi** (§3, 2026-08-10) va uchta nuqson tuzatildi; qolgani — aynan **test serverda** takrorlash (nginx, `/uploads` proxy, HTTPS, haqiqiy domenlar) |
| R3 | DevOps | Deploydan oldin **bazani zaxiralash** (migration unique indeks qo'yadi, dublikat sluglarni `-dup<Id>` qiladi) |
| R4 | Platforma egasi | Deploydan keyin mavjud mijozlarga plan biriktirish (plansiz = cheksiz) |

### 🟠 Mahsulot to'liqligi

| # | Kim | Ish |
|---|---|---|
| ~~R5~~ | ~~Frontend~~ | ~~`wms-admin` da audit sahifasi~~ — **bajarildi (F10)**, §3 dagi jadvalga qarang |
| ~~R19~~ | ~~Frontend~~ | ~~Brendlash UI~~ — **bajarildi (F9)**, §3 dagi jadvalga va `FRONTEND.md` §2.9 ga qarang |
| R20 | Ikkalasi | **Subdomen** — qaror qabul qilingan, birinchi 1–2 mijozdan keyin. `GET /api/public/branding?slug=` allaqachon tayyor. **Frontend qismi 2026-08-06 da bajarildi:** `tenant-slug.util.ts` slug'ni subdomen → eslab qolingan → standart tartibida aniqlaydi (`FRONTEND.md` §2.8), qolgani DNS/nginx |
| R21 | Ikkalasi | **S8/S9 hamkorlik** (tenantlar o'rtasida hujjat almashinuvi) — birinchi real juftlik paydo bo'lganda. Poydevor: `Organization` (S6) |
| R23 | Backend | **Avtomatlashtirilgan sinovlar yo'q.** `WMS.sln` da test loyihasi umuman mavjud emas (`dotnet test` hech narsa topmaydi) — bu fayl 2026-08-10 gacha "54/54 + 31/31 sinov" deb yozgan edi, bu noto'g'ri. Eng avval `SubscriptionPolicy.Evaluate` uchun birlik sinovlari kerak: u login, middleware va obuna endpointi uchun **yagona** qaror nuqtasi, ya'ni undagi xato uchala yo'lni ham buzadi |
| R6 | Backend | Trial tugashi haqida xabar yuborish (Telegram / in-app) — hozir fon xizmati faqat suspend qiladi, banner esa mijoz kirsagina ko'rinadi |
| ~~R7~~ | ~~Frontend~~ | ~~Limit ogohlantirishi~~ — **bajarildi (B2, B5 + F11)** |
| ~~R22~~ | ~~Frontend~~ | ~~`mustChangePassword` majburlash~~ — **bajarildi (B6 + F8)** |
| R8 | ~~Backend~~ | ~~Telefon tasdiqlash (SMS) / CAPTCHA~~ — **rejadan chiqdi**: self-service registratsiya yopilgani uchun (S3) tashqi SMS provayder kerak emas. Public lead formasi rate limit (5/soat/IP) bilan himoyalangan |
| R9 | Frontend | Plan o'zgartirish so'rovi UI (hozir faqat "biz bilan bog'laning") |
| R10 | Frontend | **Qattiq yozilgan inglizcha matnlar — o'lchangan (2026-08-06):** `notify.*` da **158**, tasdiq dialoglarida **22**, `placeholder` larda **42** (jami ~222 matn × 4 til). Bularga validatsiya ogohlantirishlari (`Recipe name is required`), enum yorliqlari (`Raw Material`, `No expiry`, `Root`) va ruxsat nomlari (`Manage Warehouse`) ham kiradi. Interceptor ustiga qo'shiladigan **ikkilangan** toastlar (12 komponent, 20 joy) allaqachon olib tashlandi |

### 🟡 Keyingi bosqich — ataylab kechiktirilgan

| # | Kim | Ish |
|---|---|---|
| R11 | Backend | **Billing:** `Subscription` / `Invoice` entity, CLICK / Payme, avtomat hisob-kitob |
| R12 | Frontend | Billing UI: hisoblar, to'lov tarixi, to'lov sahifasi |
| R13 | Backend | **AI Advisor:** Claude API — zaxira bashorati, ishlab chiqarish rejasi, chiqindi tavsiyasi. `IAiAdvisorService` stub allaqachon joyida |
| R14 | Ikkalasi | Public marketing sayt: landing, narxlar, demo |
| R15 | DevOps | Sentry + off-site backup (config-hook qoldirilgan, tashqi kalit kerak) |

> **Strategik eslatma:** manual to'lov 5–10 mijozgacha ishlaydi. Undan oshsa R11 ni
> kechiktirmang — aks holda pul yig'ishning o'zi to'siqqa aylanadi.

---

## 5. Uchma-uch sinov ssenariylari (R2)

1. **Yangi registratsiya** → Obuna sahifasi: Trial, 14 kun, 3 ta limit ko'rinadi.
2. **Sozlamalar → Modullar**: toggle yo'q, faqat holat badge'i + izoh.
3. **SuperAdmin Basic plan biriktiradi** → mijoz sahifani yangilaydi → Ishlab chiqarish
   menyudan yo'qoladi; to'g'ridan-to'g'ri URL bilan kirsa 403 "planingizga kirmaydi".
4. **SuperAdmin suspend qiladi** → mijozning navbatdagi harakati 402 → Obuna sahifasiga
   yo'naltiriladi; qayta login ham 402 → login sahifasida qizil panel.
5. **Trial sanasi o'tmishga qo'yiladi** → grace tugagach blok; oldindan banner ogohlantiradi.
6. **Trial tenantda 3-ombor yaratish** → 402 "plan limiti".

Qo'shimcha: registratsiyadan keyin sidebar faqat trial plan modullarini ko'rsatishi.

---

## 6. Deploy

### Domenlar

| Domen | Nima | Qanday |
|---|---|---|
| `app.warehouse-system.uz` | `wms-ui` (mijoz) | statik + `/api` va `/uploads` proxy |
| `admin.warehouse-system.uz` | `wms-admin` (platforma) | statik + `/api` va `/uploads` proxy |
| `api.warehouse-system.uz` | to'g'ridan-to'g'ri API / Swagger | ixtiyoriy, brauzerdagi ilovalar ishlatmaydi |

**Ikkala frontend ham `apiUrl: '/api'` — nisbiy.** Ya'ni so'rov o'z domeniga ketadi va
nginx uni backendga uzatadi. Natijada:

- **CORS umuman ishga tushmaydi** (same-origin). `appsettings.Production.json` dagi
  `Cors:AllowedOrigins` faqat zaxira — kimdir `api.` subdomeniga to'g'ridan-to'g'ri
  murojaat qiladigan bo'lsa kerak bo'ladi.
- **HTTPS'ga o'tganda hech narsa o'zgarmaydi** — protokol avtomatik mos keladi,
  mixed-content bloklanmaydi va frontendni qayta yig'ish shart emas.

### Buyruqlar

```bash
# Backend
cd wms-api && dotnet publish WMS.API -c Release -o /var/www/wms/api
# systemd: ASPNETCORE_URLS=http://localhost:7040, ASPNETCORE_ENVIRONMENT=Production

# Frontendlar
cd wms-ui    && ng build --configuration production   # → /var/www/wms-ui/browser
cd wms-admin && ng build --configuration production   # → /var/www/wms-admin/browser
```

### Nginx (ikkala frontend uchun bir xil qolip)

```nginx
server {
    listen 80;
    server_name app.warehouse-system.uz;          # admin uchun: admin.warehouse-system.uz
    root /var/www/wms-ui/browser;                 # admin uchun: /var/www/wms-admin/browser

    location /api      { proxy_pass http://localhost:7040; include proxy_params; }

    # Tenant logolari `/uploads/tenants/...` manzilida, `/api` OSTIDA EMAS.
    # Bu qatorsiz brendlash logolari 404 bo'ladi.
    location /uploads  { proxy_pass http://localhost:7040; include proxy_params; }

    location /         { try_files $uri $uri/ /index.html; }
}
```

`proxy_params` da kamida: `proxy_set_header Host $host;` ·
`X-Real-IP` · `X-Forwarded-For` · `X-Forwarded-Proto $scheme`.
Oxirgisi muhim — lead formasidagi rate limit (5/soat/IP) haqiqiy IP'ni ko'rishi kerak.

### Checklist

1. Bazani zaxiralang.
2. Dublikatlarni oldindan ko'ring:
   ```sql
   SELECT Slug, COUNT(*) FROM Tenants WHERE IsDeleted = 0 GROUP BY Slug HAVING COUNT(*) > 1;
   SELECT Code, COUNT(*) FROM Plans   WHERE IsDeleted = 0 GROUP BY Code HAVING COUNT(*) > 1;
   ```
3. `Support:Phone` / `Support:Email` ni **serverda** haqiqiysiga qo'ying (R1) —
   frontendni qayta yig'ish shart emas.
4. Prod'da `Jwt__Key` va `Seed__AdminPassword` ni **env orqali** bering
   (`appsettings.Production.json` dagi kalit repoda turibdi — uni ishlatmang).
5. `ASPNETCORE_ENVIRONMENT=Production` ni systemd unit'da **aniq** yozing. Usiz
   `.NET` Production konfiguratsiyasini olmaydi va noto'g'ri baza yo'liga uriladi.
6. Deploydan keyin `GET /api/admin/plans` → 4 ta plan, `trial` default ekanini tekshiring.
7. Mavjud mijozlarni SuperAdmin ilovasidan plan bilan bog'lang (R4).
8. Eski mijozlarda o'lchov birliklari yo'q (`TenantProvisioner` tuzatishi faqat
   yangi tenantlarga taalluqli) — kerak bo'lsa qo'lda qo'shing.

> **`wms-api/publish/` papkasini deploy uchun ishlatmang.** U git'da kuzatiladi, lekin
> ichidagi build **2026-04-12** dan qolgan (SaaS qatlami umuman yo'q) va
> `appsettings.Production.json` i ham eski — CORS'da faqat `app.` bor, baza yo'li
> `/var/www/wms-api/wms.db`. Har doim yangi `dotnet publish` qiling.

---

## 7. Agent uchun qoidalar

### Umumiy

- **Ish boshlashdan oldin** tegishli hujjatni o'qing: backend → `BACKEND.md`,
  frontend → `FRONTEND.md`. Ikkalasida ham "Konventsiyalar" bo'limi bor va u qat'iy.
- Endpoint bo'yicha yagona ishonchli manba — **Swagger** (`/swagger`), hujjat emas.
- Bir tomonni o'zgartirsangiz, ikkinchi tomondagi shartnomani tekshiring
  (DTO maydoni, xato `code`, modul kodi).

### Backend (qisqacha — batafsili `BACKEND.md` §10)

- Javob har doim `ApiResponse<T>`; xato sababi `code` maydonida.
- `TenantId` **faqat JWT'dan**. Soft delete. Pul — `decimal`. Sana — UTC.
- Xatolar: `AppException`(400) / `NotFoundException`(404) / `PaymentRequiredException`(402) /
  `ModuleDisabledException`(403).
- **Yangi plan/limit imkoniyati qo'shsangiz — server tomonidagi majburlashini ham qo'shing.**
- `Plan` hech qachon `TenantId` olmaydi.
- Build: API jarayoni `bin/` ni qulflaydi →
  `dotnet build WMS.sln -p:OutDir="<temp>\" -v q`.

### Frontend (qisqacha — batafsili `FRONTEND.md` §4)

- `inject()`, `standalone: true`, `OnPush`, faqat signals.
- HTTP faqat `ApiService`, toast faqat `NotificationService`.
- `wms-ui` da har matn **4 ta i18n fayliga ham** qo'shiladi.
- Sana uchun `date.util.ts` (`toLocalDateString`, `parseUtc`) — `toISOString()` ishlatmang.
- `error.interceptor` allaqachon toast chiqaradi — komponentda ikkinchisini qo'shmang.
- `styles.scss` o'zgarsa — **ikkala** ilovaga qo'llang.
- Har o'zgarishdan keyin `ng build --configuration production` → 0 xato.

### Tekshirmasdan aytmang

Quyidagilar avval hujjatda "ishlaydi" deb yozilgan, aslida ishlamagan edi —
shunga o'xshash da'volarni **koddan tasdiqlab** yozing:

- "Modul gating ishlaydi" — 2026-08-04 gacha faqat brauzerda edi.
- "Obuna enforcement ishlaydi" — faqat login'da edi, mavjud token 7 kun ishlayverardi.
- "Limitlar bor" — saqlanardi, lekin hech qayerda tekshirilmasdi.

---

## 8. Hujjatlar tarixi

Bu fayl 2026-08-04 da **31 ta markdown** o'rniga tuzilgan (loyiha bo'ylab tarqoq
CLAUDE.md nusxalari, bosqich hisobotlari, TODO/plan fayllari, SaaS audit hujjatlari).
Kerakli ma'lumot shu uchta faylga ko'chirilgan, qolgani o'chirilgan —
tarix `git log` da saqlanadi.

**2026-08-06 da** yopilgan bosqichlarning vazifa fayllari ham o'chirildi
(`docs/news/TASKS_*_S.md` — S1–S7/F1–F8, `docs/tasks/TASKS_*_B.md` — B1–B3/F1–F6).
Bajarilgan ishlar §3 da qayd etilgan; yakunlanmagan qismlar (R5, R7, R19) §4 dagi
o'z qatorlariga **qaror darajasida** ko'chirilgan, ya'ni yo'riqnoma emas, "nimani
unutmaslik kerak" ko'rinishida. Vazifa matnining o'zi `git log` da qoladi.

**Qoida:** bu loyihada faqat uch turdagi markdown saqlanadi — **arxitektura**,
**bajarilgan ishlar / holat** va **reestr**. Vazifa va reja fayllari yopilgach o'chiriladi.

| Fayl | Nima uchun |
|---|---|
| **`docs/CLAUDE.md`** | Kirish nuqtasi: mahsulot, holat, qolgan ishlar, deploy, agent qoidalari |
| **`docs/BACKEND.md`** | `wms-api` to'liq arxitekturasi: model, SaaS majburlash, policy, controller, konventsiya |
| **`docs/FRONTEND.md`** | `wms-ui` + `wms-admin` to'liq arxitekturasi: tuzilma, guardlar, obuna qatlami, dizayn tizimi |
| **`docs/CUSTOM_FEATURES.md`** | Bir mijoz uchun yozilgan fitchalar reestri + konvensiya (S5). Kod yonidagi uchta `Custom/README.md` shunga havola qiladi |
