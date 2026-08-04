# Frontend vazifalari — S1–S7 (soddalashtirilgan SaaS modeli)

> **Terminal:** frontend (`wms-ui` + `wms-admin`) · **Branch:** `saas-admin`
> **Juftlik fayli:** `TASKS_BACKEND_S.md` — backend alohida terminalda shu shartnoma bo'yicha ishlaydi.
> **Oldingi bosqich:** F1–F7 bajarilgan (`SAAS_STATUS.md` §3). Bu fayl **ustiga** quriladi.

---

## 0. Kontekst — biznes modeli o'zgardi

Avvalgi qurilish "self-service SaaS" edi. Yangi model:

1. Mijoz bilan telefonda gaplashamiz → demo → to'lov → **biz** admin paneldan tenant yaratamiz.
2. To'lov muddati tugasa avtomatik to'xtaydi. Masofadan yoqish/o'chirish/**vaqtincha** to'xtatish.
3. Har mijozga kerakli menyuni admin paneldan alohida ochamiz/yopamiz.
4. Ayrim mijozga maxsus fitcha — faqat unga ko'rinadi.
5. Mijozning ta'minotchi/xaridorlarini keyinchalik alohida tenant qilib sotamiz.

Public registratsiya **yopiladi**, o'rniga "demo so'rash" formasi. To'lov integratsiyasi hozircha yo'q.

### Umumiy qoidalar

- Har vazifadan keyin ikkala ilova ham prod build: `ng build --configuration production` → **0 xato**.
- **i18n:** `wms-ui` da har yangi matn 4 tilga ham qo'shilsin (kalitlar soni farq qilmasin).
  `wms-admin` — faqat ingliz tilida, Transloco yo'q.
- Angular signals, `inject()`, standalone, `OnPush` — mavjud konvensiya saqlansin.
- Xato matnlari **`err.error.code`** bo'yicha tanlansin, HTTP statusi bo'yicha emas.
- Sana maydonlari backenddan UTC keladi — ko'rsatishda mahalliy vaqtga o'giring.

### Tartib va parallellik

```
S1 → S2 → S3      (ketma-ket)
S6                (mustaqil — istalgan vaqtda)
S4 → S5           (S4 tugamasdan S5 boshlanmaydi)
S7                (S3 tugagach)
```

**Backend'ni kutmang.** Quyidagi §Shartnoma to'liq va o'zgarmas. Servis qatlamini shartnomaga
qarab yozing; backend endpointi hali tayyor bo'lmasa mock bilan ishlang, oxirida ulang.

---

# 🔴 S1 — To'langan muddat: admin boshqaruvi va mijoz ko'rinishi

## Maqsad

SuperAdmin to'lovni qayd etadi va muddati tugayotgan mijozlarni bir qarashda ko'radi.
Mijoz o'z muddatini biladi va oldindan ogohlantiriladi.

## `wms-admin`

### Tenantlar jadvali

- Yangi **"Paid until"** ustuni. Rang qoidasi (mavjud "Trial ends" ustunidagi bilan bir xil uslub):
  o'tgan → qizil + "expired" yorlig'i · 3 kundan kam → qizil · 7 kundan kam → sariq · aks holda oddiy.
  `null` → `—` va "no limit" tooltip'i.
- Filtr: `Expiring soon (7d)` / `Expired` / `No limit`.

### To'lov qayd etish dialogi

Tenant qatoridagi amal menyusidan **"Record payment"**:

| Maydon | Xatti-harakat |
|---|---|
| Davr boshi / oxiri | Ikkita datepicker. Tez tugmalar: **+1 oy**, **+3 oy**, **+6 oy**, **+1 yil** — boshini mavjud `paidUntil` (yoki bugun) dan oladi |
| Summa | Raqam, mingliklar ajratilgan holda ko'rsatilsin |
| Valyuta | Default `UZS` |
| Usul | `Cash` / `BankTransfer` / `Card` / `Other` |
| Izoh | Matn |

Yuborilgach: jadval yangilansin, muvaffaqiyat xabari **davr oxirini** ko'rsatsin
("Paid until 1 Sep 2026"). Tenant `NonPayment` sababli suspend bo'lgan bo'lsa —
"Tenant reactivated" deb alohida bildirilsin.

### To'lov tarixi

Tenant detail'da yangi tab: sana, davr, summa, usul, izoh, kim qayd etgan.
Har qatorda "bekor qilish" (tasdiqlash dialogi bilan — `PaidUntil` qayta hisoblanishini ogohlantiring).

### Dashboard

Yangi karta: **"Expiring soon"** — 7 kun ichida tugaydigan tenantlar soni,
bosilganda filtrlangan jadvalga o'tadi. Yonida **"Expired"** kartasi.

## `wms-ui`

### Obuna sahifasi (`/settings/subscription`)

Mavjud sahifaga: `paidUntil` bo'lsa uni asosiy sana sifatida ko'rsating (trial emas).
Qolgan kun soni, grace davri haqida bir qator izoh.
Trial va paid bir vaqtda bo'lsa — **paid ustun** (mijoz to'lagan, demo tugagan).

### Banner

Mavjud `subscription-banner` komponentiga: to'lov muddati tugashiga **7 kundan kam** qolsa
sariq banner — "To'lov muddati X kundan keyin tugaydi" + aloqa ma'lumoti.
Kuniga bir marta yopiladi (mavjud mantiq); blok holatida yopilmaydi.

## Qabul mezoni

- To'lov qayd etilgach jadvaldagi sana va rang darhol yangilanadi.
- Tez tugmalar sanani to'g'ri hisoblaydi (mavjud `paidUntil` dan davom ettiradi).
- Mijoz Obuna sahifasida to'lov muddatini ko'radi; 7 kun qolganda banner chiqadi.
- `paidUntil = null` bo'lgan tenantda hech qanday ogohlantirish ko'rinmaydi.

---

# 🔴 S2 — Sabab bilan va vaqtincha to'xtatish

## Maqsad

Suspend endi sababga ega va muddatli bo'lishi mumkin. Mijoz **aniq nima bo'lganini** ko'rsin.

## `wms-admin`

### Suspend dialogi (hozir tasdiqlash oynasi — to'liq formaga aylantiring)

| Maydon | Izoh |
|---|---|
| Sabab | `NonPayment` / `ClientRequest` / `Technical` / `Violation` / `Other` — majburiy |
| Ichki izoh | Faqat siz ko'rasiz. Formada aniq yozilsin: **"Internal — not shown to the client"** |
| Mijozga xabar | Ixtiyoriy. Ostida jonli ko'rinish: mijoz aynan shu matnni ko'radi |
| Vaqtincha | Checkbox → ochilganda datepicker "Auto-reactivate on". Yonida "Tenant will be reactivated automatically" izohi |

### Jadval

Suspend badge'i sabab bilan (`Suspended · Non-payment`).
`suspendedUntil` bo'lsa qatorda "until 1 Oct" ko'rsatilsin.
Filtr: `Suspended` ichida sabab bo'yicha kichik filtr.

## `wms-ui`

### Blok holati

Mavjud 402 ishlovi kengaytirilsin — har `code` uchun alohida matn:

| `code` | Ko'rsatiladigan matn (mazmuni) |
|---|---|
| `payment_expired` | To'lov muddati tugagan — aloqa ma'lumoti bilan |
| `suspended_nonpayment` | To'lov sababli to'xtatilgan |
| `suspended_request` | So'rovingizga binoan to'xtatilgan |
| `suspended_technical` | Texnik ishlar olib borilmoqda |
| `suspended_violation` | Tizim to'xtatilgan — bog'laning |
| `suspended_other` | Tizim to'xtatilgan — bog'laning |
| `trial_expired` | Demo muddati tugagan |

Backend `blockedMessage` yuborgan bo'lsa — **o'sha ustun**, standart matn o'rniga.
`suspendedUntil` bo'lsa qo'shimcha qator: "Tizim {sana} da avtomatik qayta yoqiladi".

Login sahifasidagi mavjud qizil panel ham shu kodlarni qo'llab-quvvatlasin.

## Qabul mezoni

- Sabab tanlanmasa suspend tugmasi ishlamaydi.
- Ichki izoh mijozning hech qaysi ekranida ko'rinmaydi.
- Vaqtincha to'xtatishda mijoz avtomatik yoqilish sanasini ko'radi.
- Har olti `code` uchun to'g'ri matn chiqadi (qo'lda tekshiring).

---

# 🔴 S3 — Registratsiya o'rniga demo so'rovi

## Maqsad

`/register` sahifasi endi tenant yaratmaydi — u sizga qo'ng'iroq qilish uchun lead qoldiradi.
Admin panelda lead'lar ro'yxati va "tenantga aylantirish" tugmasi paydo bo'ladi.

## `wms-ui`

### `/register` → demo so'rovi

Mavjud sahifani qayta ishlang (route'ni saqlang, `/request-demo` ga `redirect` qo'shing):

- Maydonlar: kompaniya nomi, aloqa uchun ism, telefon (majburiy), email (ixtiyoriy), izoh.
- Slug, parol, modul tanlash — **hammasi olib tashlanadi**.
- Yuborilgach: forma o'rniga muvaffaqiyat ekrani — "So'rovingiz qabul qilindi, 24 soat ichida
  bog'lanamiz" + aloqa telefoni. Qayta yuborish tugmasi bo'lmasin.
- 429 (rate limit) → "Ko'p so'rov yuborildi, keyinroq urinib ko'ring".
- Login sahifasidagi "Ro'yxatdan o'tish" havolasi matni **"Demo so'rash"** ga o'zgarsin.

## `wms-admin`

### Yangi bo'lim: **Leads**

Sidebar'ga qo'shing (Tenants va Plans yonida).

**Jadval:** kompaniya, aloqa ismi, telefon, manba (`Website` / `Portal` / `Manual` / `Referral`),
"kim orqali" (referrer tenant — portal lead'lari uchun), status, kelgan sana.
Status badge'lari rang bilan: `New` ko'k · `Contacted` sariq · `DemoGiven` binafsha ·
`Won` yashil · `Lost` kulrang.

**Filtrlar:** status, manba, qidiruv (kompaniya/telefon), sana oralig'i.
Default ko'rinish: `New` birinchi, eng yangisi tepada.

**Detail drawer:** to'liq ma'lumot, status o'zgartirish (izoh bilan), izohlar tarixi.

**"Convert to tenant" tugmasi:** tenant yaratish formasini ochadi, kompaniya nomi va aloqa
ma'lumoti oldindan to'ldirilgan holda. Qo'shimcha: slug, plan, admin login/parol, `paidUntil`.
Muvaffaqiyatda lead `Won` bo'ladi va yaratilgan tenantga havola ko'rsatiladi.
Slug band bo'lsa — formada xato, lead o'zgarmaydi.

### Dashboard

Yangi karta: **"New leads"** — javob berilmagan so'rovlar soni, bosilganda Leads bo'limiga o'tadi.

## Qabul mezoni

- `/register` endi tenant yaratmaydi, faqat so'rov yuboradi.
- Lead admin panelda darhol ko'rinadi.
- Convert haqiqiy ishlaydigan tenant yaratadi va lead `Won` bo'ladi.
- Portal orqali kelgan lead'da "kim orqali" ustuni to'ldirilgan (S7 dan keyin tekshiriladi).

---

# 🔴 S4 — Feature qatlami: menyu darajasidagi boshqaruv

## Maqsad

Modul (11 ta) juda yirik dona. Endi menyu darajasida — A mijozga bitta sahifani ochish/yopish.
Uch qatlam: **Plan** (nima sotildi) → **Feature** (tenantda bormi) → **Permission** (kim ishlatadi).

## `wms-ui`

### Auth va holat

- `AuthService` ga `enabledFeatures` signali (login javobi va `/subscription/me` dan).
- Yangi `featureGuard(code)` — mavjud `moduleGuard` bilan bir xil uslubda.
  Rad etilganda mavjud "planingizga kirmaydi" sahifasiga yo'naltirsin.
- **Sidebar** feature bo'yicha ham filtrlansin. Tartib muhim: avval modul, keyin feature.
  Modul o'chiq bo'lsa uning butun bo'limi ko'rinmaydi (mavjud xatti-harakat).
- Dev-fallback ro'yxati (mavjud 11 modulli ro'yxat yonida) — barcha feature kodlari bilan
  to'ldirilsin, aks holda lokal ishlashda menyu bo'sh qoladi.

### Route'lar

Feature'ga tegishli har route'ga `featureGuard` qo'shing. Feature kodlari backend seed'i bilan
**aynan** mos bo'lishi shart — `TASKS_BACKEND_S.md` §S4 dagi ro'yxatga qarang.

### Obuna sahifasi

Mavjud "modul chiplari" yoniga: yoqilgan feature'lar modul bo'yicha guruhlangan holda,
yig'iladigan panelda. Uzun ro'yxat asosiy ko'rinishni bosib ketmasin.

### Xato ishlovi

`error.interceptor` da `403 feature_disabled:CODE` → "Bu imkoniyat sizning tarifingizga kirmaydi"
(mavjud `module_disabled` ishlovi bilan bir xil uslubda).

## `wms-admin`

### Tenant detail → yangi **"Features"** tab

- Modul bo'yicha guruhlangan ro'yxat (akkordeon). Modul o'chiq bo'lsa butun guruh kulrang va
  ustida izoh: **"Module disabled — features have no effect"**.
- Har feature qatorida uch holatli boshqaruv:

| Holat | Ma'nosi | Ko'rinishi |
|---|---|---|
| **Plan** | Override yo'q, plandagidek | Kulrang, joriy qiymat ko'rsatiladi |
| **On** | Majburan yoqilgan | Yashil + `override` badge |
| **Off** | Majburan o'chirilgan | Qizil + `override` badge |

- Plandagi qiymatdan farq qilsa — **"outside plan"** badge (mavjud modullar dialogidagi uslub).
- Har override uchun ixtiyoriy izoh maydoni (nega ochildi).
- Yuqorida: "Reset all to plan" tugmasi (tasdiqlash bilan).
- Qidiruv maydoni — 25+ feature bo'lgani uchun majburiy.

### Plan formasi

Modul tanlash yoniga feature tanlash bo'limi — modul bo'yicha guruhlangan checkbox'lar.
Modul tanlanmagan bo'lsa uning feature'lari o'chirilgan (disabled) ko'rinsin.

## Qabul mezoni

- S4 seed'idan keyin hech bir mijozning menyusi o'zgarmagan (regressiyani aniq tekshiring).
- Admin paneldan feature o'chirilsa — mijozda menyu yo'qoladi va to'g'ridan-to'g'ri URL 403 beradi.
- "Plan" holatiga qaytarish ishlaydi va badge yo'qoladi.
- Modul o'chirilganda feature boshqaruvi ta'sir qilmasligi UI'da tushunarli ko'rsatilgan.

---

# 🟠 S5 — Maxsus (custom) feature konvensiyasi

## Maqsad

C mijozga faqat unga ko'rinadigan fitcha — alohida branch va alohida deploysiz.
Bu vazifada **haqiqiy fitcha yozilmaydi**, faqat skelet va qoidalar.

## `wms-ui`

### Papka konvensiyasi

```
src/app/modules/custom/<feature-code>/
```

Har custom modul:
- **Lazy** yuklanadi (`loadComponent` / `loadChildren`).
- Route'da `featureGuard('custom.<code>')` **majburiy**.
- Sidebar yozuvi ham shu feature bilan shartlangan.
- i18n kalitlari `custom.<code>.*` prefiksi bilan — umumiy fayllarni ifloslantirmasin.

Umumiy komponentlarga (`shared/`) custom mantiq **yozilmasin**.

### Namuna skelet

Bitta ishlaydigan namuna qoldiring: `custom/example-feature/` — bo'sh sahifa,
route, guard va i18n bilan. Keyingi custom fitcha shundan ko'chiriladi.

## `wms-admin`

- Features tab'ida **"Custom"** filtri — maxsus feature'lar alohida ko'rinsin.
- Custom feature qatorida: kim uchun yozilgan (`OwnerTenantId`), qachon so'ralgan, sabab.
- Boshqa tenantga custom feature yoqishga urinilsa — ogohlantirish dialogi:
  "This feature was built for another client. Enable anyway?"

## Qabul mezoni

- Namuna custom feature yoqilganda ko'rinadi, o'chirilganda menyudan yo'qoladi va URL 403 beradi.
- Custom feature'lar umumiy i18n fayllarini kattalashtirmaydi.
- `CUSTOM_FEATURES.md` (backend tomonda yaratiladi) frontend fayllarini ham ro'yxatga oladi.

---

# 🟠 S6 — STIR (INN) va kompaniya identifikatori

## Maqsad

Real kompaniyani platforma darajasida yagona identifikator bilan tanish. Bugun bu bir kunlik ish;
ming counterparty yig'ilgandan keyin oylik loyihaga aylanadi.

Bu vazifa hamkorlik va hujjat almashinuvi (S8/S9) uchun **poydevor** — hozir faqat maydon va bog'lash.

## `wms-ui`

### Counterparty formasi

- Yangi **INN (STIR)** maydoni. Majburiy emas, lekin yonida izoh:
  "Kelajakda hamkor bilan avtomatik bog'lanish uchun kerak".
- Validatsiya: **9 raqam** yoki bo'sh. Boshqa format → forma xatosi.
- Jadvalga INN ustuni + qidiruvga INN bo'yicha qidirish.
- Excel import shablonida INN ustuni.

### "Platformada ro'yxatdan o'tgan" indikatori

Backend counterparty javobida `organizationId` va `isPlatformTenant` (bog'langan Organization
tenantga ega ekanmi) qaytaradi. Shu bo'lsa qatorda kichik yashil belgi + tooltip:
"Bu kompaniya platformada o'z tizimiga ega".

Hozircha faqat **ko'rsatkich** — hech qanday amal bog'lanmaydi. S8 da shu joydan
"hamkorlik o'rnatish" tugmasi chiqadi.

## `wms-admin`

- Tenant yaratish/tahrirlash formasiga INN maydoni (o'sha validatsiya bilan).
- Yangi **Organizations** ro'yxati: INN, nom, telefon, nechta tenant bog'langan,
  nechta counterparty yozuvida uchraydi.
- Organization detail: bog'langan tenantlar va qaysi tenantlarda counterparty sifatida borligi.
  Bu sizning "kim kim bilan ishlaydi" ko'rinishingiz — upsell uchun asosiy vosita.

## Qabul mezoni

- Noto'g'ri INN kiritilsa forma yuborilmaydi.
- INN siz counterparty yaratish ishlaydi.
- Bir xil INN li counterparty ikki tenantda bir Organization ga bog'lanadi (admin panelda ko'rinadi).
- Indikator faqat haqiqiy tenant bo'lgan kompaniyalarda chiqadi.

---

# 🟠 S7 — Portal foydalanuvchisiga to'liq versiyani taklif qilish

## Maqsad

A ning portalidagi ta'minotchi/xaridorlar — tizimni ko'rgan tayyor lead'lar.
Ularga to'liq versiyani ko'rsatamiz. **S3 tugagan bo'lishi shart.**

## `wms-ui` — portal qismi

### Banner

Counterparty portali va agent portali dashboard'ida yuqorida banner:

- Sarlavha: to'liq WMS haqida bir qator (o'z omboringiz, ishlab chiqarish, hisobotlar).
- **"Qiziqaman"** tugmasi.
- Yopilishi mumkin (`localStorage` da yopilgan holat, 14 kundan keyin qayta ko'rinadi).
- Uslub: reklama emas, **taklif** tuyulsin — mavjud dizayn tillari bilan mos, tinch ranglar.
  Portal foydalanuvchisi A ning mijozi, agressiv reklama A ga zarar qiladi.

### Forma

Tugma → kichik dialog: telefon (tokendan oldindan to'ldirilgan bo'lsa ham tahrirlanadigan) + izoh.
Yuborilgach banner o'rniga: "So'rovingiz qabul qilindi — tez orada bog'lanamiz".

Sahifa qayta ochilganda ham shu holat ko'rinsin (`GET /api/portal/upgrade-interest` orqali).

### Statik sahifa

Portal ichida `/portal/full-version` — to'liq versiya imkoniyatlari haqida bir sahifa
(modullar ro'yxati, ekran suratlari o'rniga hozircha matn va ikonalar). Banner shunga havola qilsin.

## `wms-admin`

Leads jadvalida (S3 da yaratilgan) manba `Portal` bo'lgan qatorlar ajralib tursin:
"kim orqali" ustunida referrer tenant nomi, bosilganda o'sha tenantga o'tadi.

## Qabul mezoni

- Banner portal dashboard'ida ko'rinadi, yopilsa 14 kun qaytmaydi.
- So'rov yuborilgach holat saqlanadi (sahifa yangilanganda ham).
- Lead admin panelda `Portal` manbasi va referrer tenant bilan ko'rinadi.
- Banner asosiy WMS ilovasida (portal emas) **hech qachon ko'rinmaydi**.

---

# API shartnomasi — backend shu shaklni beradi

> Bu bo'lim `TASKS_BACKEND_S.md` da **aynan takrorlangan**. O'zgartirsangiz — ikkalasida.

## `GET /api/subscription/me` (to'liq shakl)

```json
{
  "success": true,
  "data": {
    "tenantName": "Ice Gold",
    "planName": "Pro",
    "planCode": "pro",
    "status": "Active",

    "trialEndsAt": null,
    "daysUntilTrialEnd": null,

    "paidUntil": "2026-09-01T00:00:00Z",
    "daysUntilPaidEnd": 28,
    "paymentGraceDays": 3,

    "isBlocked": false,
    "blockedReason": null,
    "blockedMessage": null,
    "suspendedUntil": null,

    "limits": {
      "maxUsers": 25,        "currentUsers": 7,
      "maxWarehouses": 10,   "currentWarehouses": 3,
      "maxTransfersPerMonth": 10000, "currentTransfersThisMonth": 412
    },

    "enabledModules": ["WAREHOUSE_RAW", "TRANSFERS", "PRODUCTION"],
    "enabledFeatures": ["production.orders", "production.recipes", "export.excel"],

    "supportPhone": "+998 ...",
    "supportEmail": "..."
  }
}
```

`isBlocked = true` bo'lganda ham **200** qaytadi — mijoz sababni ko'rishi shart.

## Xato kodlari (`err.error.code`)

| HTTP | `code` | Frontend nima qiladi |
|---|---|---|
| 402 | `trial_expired` | Blok ekrani + Obuna sahifasiga yo'naltirish |
| 402 | `payment_expired` | Blok ekrani + aloqa ma'lumoti |
| 402 | `suspended_nonpayment` | Blok ekrani |
| 402 | `suspended_request` | Blok ekrani |
| 402 | `suspended_technical` | Blok ekrani |
| 402 | `suspended_violation` | Blok ekrani |
| 402 | `suspended_other` | Blok ekrani |
| 402 | `tenant_inactive` | Blok ekrani |
| 402 | `limit_users` / `limit_warehouses` / `limit_transfers` | Faqat toast, yo'naltirish yo'q |
| 403 | `module_disabled:CODE` | "Tarifingizga kirmaydi" |
| 403 | `feature_disabled:CODE` | "Tarifingizga kirmaydi" |

Backend `blockedMessage` yuborsa — standart matn o'rniga **o'sha** ko'rsatiladi.

## Yangi endpointlar

**Admin:**
```
POST   /api/admin/tenants/{id}/payments      { periodStart, periodEnd, amount, currency, method, note }
GET    /api/admin/tenants/{id}/payments
DELETE /api/admin/payments/{paymentId}
GET    /api/admin/tenants/expiring?days=7

PUT    /api/admin/tenants/{id}/suspend       { reason, note, publicMessage, until }

GET    /api/admin/leads?status=&source=&search=&page=&pageSize=
GET    /api/admin/leads/{id}
PUT    /api/admin/leads/{id}                 { status, statusNote }
POST   /api/admin/leads/{id}/convert         tenant yaratish DTO'si

GET    /api/admin/features
GET    /api/admin/tenants/{id}/features      [{ code, name, moduleCode, isEnabled, source, note }]
PUT    /api/admin/tenants/{id}/features      { features: [{ code, isEnabled, note }] }

GET    /api/admin/organizations?search=
GET    /api/admin/organizations/{id}
```

`source` qiymatlari: `"tenant"` (override) · `"plan"` · `"default"`.
`isEnabled: null` yuborilsa override o'chiriladi.

**Public / portal:**
```
POST /api/leads                      { companyName, contactName, phone, email?, note? }
POST /api/portal/upgrade-interest    { phone, note? }
GET  /api/portal/upgrade-interest
```

---

# Yakuniy tekshiruv (barcha S vazifalardan keyin)

1. `wms-ui` va `wms-admin` prod build → 0 xato.
2. i18n: 4 fayl, kalitlar soni **teng** (farqni skript bilan tekshiring).
3. Regressiya: S4 dan keyin mavjud mijozning menyusi o'zgarmagan.
4. Uchma-uch: demo so'rovi → admin lead → convert → to'lov qayd → feature yoq/o'chir →
   suspend (sabab bilan) → mijoz to'g'ri xabarni ko'radi → avtomatik qayta yoqilish.
5. **R1 (unutilmasin):** `environment.ts` va `environment.prod.ts` da `supportPhone` /
   `supportEmail` hali placeholder (`+998 90 000 00 00`). Bloklangan mijoz aynan shuni ko'radi —
   haqiqiysiga almashtiring.
6. `SAAS_STATUS.md` §3 ni yangilang.
