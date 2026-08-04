# CLAUDE.md — WMS Platform

> **Oxirgi yangilanish:** 2026-08-04 · **Branch:** `saas-admin`
> **Bu fayl — loyihaning kirish nuqtasi va yagona manbasi.**
> Texnik tafsilotlar ikki qo'shni hujjatda:
> **[`BACKEND.md`](BACKEND.md)** (`wms-api`) · **[`FRONTEND.md`](FRONTEND.md)** (`wms-ui` + `wms-admin`).
> Boshqa hujjat yo'q — hammasi shu uchtasiga birlashtirilgan.

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
└── wms-admin/     # Angular 21 · SuperAdmin konsoli · inglizcha
```

### Texnologiya

| Qatlam | Texnologiya |
|---|---|
| Backend | .NET 8 LTS, ASP.NET Core Web API, EF Core 8, SQLite, JWT, Serilog |
| Frontend | Angular 21 (standalone, signals, **zoneless**), PrimeNG 21.1.5 (Aura), Tailwind v4 |
| Grafik | ApexCharts + ng-apexcharts |
| i18n | Transloco — `uz`, `uz-cyrl`, `ru`, `en` (faqat `wms-ui`) |
| IDE | Rider (backend), WebStorm (frontend) |

---

## 3. Hozirgi holat (2026-08-04)

**Cheklovlar API darajasida majburlanadi va mijozga tushunarli ko'rsatiladi.**
Modul gating backendda, obuna har so'rovda tekshiriladi, trial haqiqiy muddat bilan
ishlaydi, plan limitlari amalda; mijoz o'z obunasini va bloklanish sababini ko'radi.

| Tomon | Build | Bajarilgan |
|---|---|---|
| `wms-api` | 0 xato, 0 ogohlantirish · 29/29 sinov | SaaS majburlash: modul gate, obuna middleware, trial+grace, limitlar, unique indeks, planlar seed, platforma audit izi |
| `wms-ui` | prod 785 kB, 0 xato | Obuna sahifasi + banner, modullar faqat-ko'rish, 402/403 kod bo'yicha xato boshqaruvi, yangi modul guardlari |
| `wms-admin` | prod 677 kB, 0 xato | Trial ustuni, "outside plan" badge, plan `trialDays`/`isDefault`, texnik qarz tozalandi |
| i18n | 4 til × 574 kalit, farq yo'q | — |

**Asosiy kelishuv:** plani **bor** tenant → plan modullari va limitlari;
plani **yo'q** tenant → **cheksiz** (mavjud mijozlar ishi to'satdan to'xtamasin).

### Nima allaqachon tayyor (mahsulot bo'yicha)

Core WMS (ombor, partiya/LOT + FEFO, transfer, ishlab chiqarish retsept/buyurtma/bosqich,
moliya + qarz, KPI + smena + davomat, sifat nazorati, mahsulot/kontragent) ·
Agent moduli (komissiya) · Delivery moduli (transport, haydovchi, marshrut, yuk xati PDF) ·
Qaytarish (return) · Audit jurnali · Bildirishnomalar + Telegram (config-gated) ·
Excel import/export · PDF · Barcode · Analitika (11 endpoint) · Kontragent va agent portallari ·
Self-service registratsiya · Kunlik DB backup · `/health` + Serilog · CI/CD · Performance indekslar.

---

## 4. Qolgan ishlar

### 🔴 Sotuvga chiqishdan oldin majburiy

| # | Kim | Ish |
|---|---|---|
| R1 | Frontend | `environment.ts` / `environment.prod.ts` da **haqiqiy** `supportPhone` / `supportEmail` (hozir placeholder — bloklangan mijoz aynan shuni ko'radi) |
| R2 | Ikkalasi | **Jonli muhitda uchma-uch sinov** — §5 dagi 6 ssenariy |
| R3 | DevOps | Deploydan oldin **bazani zaxiralash** (migration unique indeks qo'yadi, dublikat sluglarni `-dup<Id>` qiladi) |
| R4 | Platforma egasi | Deploydan keyin mavjud mijozlarga plan biriktirish (plansiz = cheksiz) |

### 🟠 Mahsulot to'liqligi

| # | Kim | Ish |
|---|---|---|
| R5 | Frontend | `isPlatformAction` hech qaysi ilovada ko'rsatilmaydi (`wms-ui` audit sahifasida badge, `wms-admin` da audit sahifasi umuman yo'q) |
| R6 | Backend | Trial tugashi haqida xabar yuborish (Telegram / in-app) — hozir fon xizmati faqat suspend qiladi, banner esa mijoz kirsagina ko'rinadi |
| R7 | Ikkalasi | Limit 80 % ga yetganda ogohlantirish (hozir faqat progress-bar rangi, yaratish paytida ogohlantirish yo'q) |
| R8 | Backend | Telefon tasdiqlash (SMS) / CAPTCHA — tashqi provayder (Eskiz / Play Mobile) kerak. **Public marketingdan oldin.** Rate limiting va slug qora ro'yxati allaqachon bor |
| R9 | Frontend | Plan o'zgartirish so'rovi UI (hozir faqat "biz bilan bog'laning") |
| R10 | Frontend | Ba'zi CRUD toast matnlari hali qattiq yozilgan (i18n qamroviga kirmagan) |

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

```bash
# Backend
cd wms-api && dotnet publish WMS.API -c Release -o /var/www/wms-api
# systemd: ASPNETCORE_URLS=http://localhost:7040, ASPNETCORE_ENVIRONMENT=Production

# Frontendlar (alohida hostlar)
cd wms-ui    && ng build --configuration production   # → /var/www/wms-ui
cd wms-admin && ng build --configuration production   # → /var/www/wms-admin (subdomen)

# Nginx: / → dist/<app>/browser (try_files ... /index.html), /api → localhost:7040
```

**Checklist**

1. Bazani zaxiralang.
2. Dublikatlarni oldindan ko'ring:
   ```sql
   SELECT Slug, COUNT(*) FROM Tenants WHERE IsDeleted = 0 GROUP BY Slug HAVING COUNT(*) > 1;
   SELECT Code, COUNT(*) FROM Plans   WHERE IsDeleted = 0 GROUP BY Code HAVING COUNT(*) > 1;
   ```
3. `environment.prod.ts` da aloqa ma'lumotini haqiqiysiga almashtiring (R1).
4. Prod'da `Jwt__Key` va `Seed__AdminPassword` ni **env orqali** bering.
5. Deploydan keyin `GET /api/admin/plans` → 4 ta plan, `trial` default ekanini tekshiring.
6. Mavjud mijozlarni SuperAdmin ilovasidan plan bilan bog'lang (R4).

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

| Fayl | Nima uchun |
|---|---|
| **`docs/CLAUDE.md`** | Kirish nuqtasi: mahsulot, holat, qolgan ishlar, deploy, agent qoidalari |
| **`docs/BACKEND.md`** | `wms-api` to'liq arxitekturasi: model, SaaS majburlash, policy, controller, konventsiya |
| **`docs/FRONTEND.md`** | `wms-ui` + `wms-admin` to'liq arxitekturasi: tuzilma, guardlar, obuna qatlami, dizayn tizimi |
