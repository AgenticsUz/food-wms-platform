# Backend vazifalari — S1–S7 (soddalashtirilgan SaaS modeli)

> **Terminal:** backend (`wms-api`) · **Branch:** `saas-admin`
> **Juftlik fayli:** `TASKS_FRONTEND_S.md` — frontend alohida terminalda shu shartnoma bo'yicha ishlaydi.
> **Oldingi bosqich:** T1–T13 bajarilgan (`BACKEND_SAAS_REPORT.md`). Bu fayl **ustiga** quriladi.

---

## 0. Kontekst — biznes modeli o'zgardi

Avvalgi qurilish "self-service SaaS" edi. Yangi model:

1. Mijoz bilan telefonda gaplashamiz → demo beramiz → to'laydi → **biz** admin paneldan tenant yaratamiz.
2. To'lov muddati tugasa — avtomatik to'xtaydi. Masofadan yoqish/o'chirish/**vaqtincha** to'xtatish kerak.
3. Har mijozga kerakli menyu/funksiyani admin paneldan alohida ochamiz yoki yopamiz.
4. Ayrim mijozga maxsus fitcha yozamiz — faqat unga ko'rinadi, alohida branch yo'q.
5. Mijozning o'z ta'minotchi/xaridorlarini keyinchalik alohida tenant qilib sotamiz.

Public self-service registratsiya **yopiladi**. To'lov integratsiyasi (CLICK/Payme) **hozircha yo'q**.

### Umumiy qoidalar

- Har vazifadan keyin `dotnet build WMS.sln` → **0 xato, 0 ogohlantirish**.
- Mavjud mijozlarning ishi to'satdan to'xtamasin: yangi maydonlar `null` bo'lsa **cheklov yo'q**.
- Yangi cheklov qo'shsangiz — server tomonida majburlashni **o'sha commitda** yozing (`CLAUDE.md` §18 qoidasi).
- SuperAdmin barcha yangi cheklovlardan mustasno.
- Barcha xato javoblari `ApiResponse.Code` maydonini to'ldirsin — frontend shunga qarab matn ko'rsatadi.
- Sana maydonlari backendda **UTC**.

### Tartib va sinxronlash nuqtalari

```
S1 → S2 → S3      (ketma-ket, bir-biriga tayanadi)
S6                (mustaqil — istalgan vaqtda parallel qilinishi mumkin)
S4 → S5           (S4 tugamasdan S5 boshlanmaydi)
S7                (S3 tugagach)
```

**Frontend bilan sinxronlash:** har vazifa boshida avval DTO va endpointlarni (bo'sh implementatsiya bilan)
merge qiling, keyin mantiqni yozing. Frontend shartnomaga qarab parallel ishlaydi.

---

# 🔴 S1 — To'langan muddat va avtomatik to'xtatish

## Maqsad

Manual billingning asosi. SuperAdmin "shu sanagacha to'langan" deb belgilaydi; muddat tugasa tizim
o'zi to'xtatadi; mijoz oldindan ogohlantiriladi. Busiz har oy qo'lda kuzatib yurishga to'g'ri keladi.

## Bajariladigan ishlar

### Domen

- `Tenant` ga qo'shing: `PaidUntil` (`DateTime?`).
- Yangi entity `PaymentRecord` — manual to'lov qaydi. Bu keyinchalik billing'ga o'sadigan urug'.
  Maydonlar: `TenantId`, `PeriodStart`, `PeriodEnd`, `Amount` (`decimal`), `Currency` (default `UZS`),
  `Method` (enum: `Cash`, `BankTransfer`, `Card`, `Other`), `Note`, `RecordedByUserId`, `RecordedAt`.
  `BaseEntity` dan meros oladi, lekin **platforma darajasida** — `TenantId` bu yerda maqsad tenant,
  global filtr qo'llanmaydi (SuperAdmin hammasini ko'radi).

### Majburlash

- `SubscriptionPolicy` ga yangi shart: tenantning `PlanId` bor **va** `SubscriptionStatus == Active`
  **va** `PaidUntil != null` **va** `PaidUntil + PaidGraceDays < now` → blok, `code = "payment_expired"`.
- `PaidUntil == null` → **blok yo'q** (mavjud mijozlar va tizim tenanti buzilmasin).
- `SubscriptionExpiryBackgroundService` endi ikkita narsani tekshirsin:
  trial muddati (mavjud mantiq) **va** `PaidUntil + grace` o'tgan tenantlarni `Suspended` ga o'tkazsin,
  `SuspendReason = NonPayment` bilan (S2 dan keyin bu maydon bo'ladi — S2 ni S1 dan keyin qilsangiz
  shu joyni to'ldirishni unutmang).
- Ma'lumot **hech qachon o'chirilmaydi**.

### Konfiguratsiya

`appsettings.json` → `Subscription` bo'limiga: `PaidGraceDays` (default `3`).
Mavjud `WarnBeforeDays` (7) frontend banneri uchun ishlatiladi.

### API

**Admin (SuperAdmin):**

```
POST   /api/admin/tenants/{id}/payments      to'lov qayd etish
GET    /api/admin/tenants/{id}/payments      to'lov tarixi (sahifalangan)
DELETE /api/admin/payments/{paymentId}       xato yozuvni bekor qilish (soft delete)
GET    /api/admin/tenants/expiring?days=7    muddati tugayotganlar (dashboard uchun)
```

`POST .../payments` tanasi:

```json
{
  "periodStart": "2026-08-01T00:00:00Z",
  "periodEnd":   "2026-09-01T00:00:00Z",
  "amount":      2900000,
  "currency":    "UZS",
  "method":      "BankTransfer",
  "note":        "Avgust oyi uchun"
}
```

Xatti-harakat: yozuv yaratiladi **va** `tenant.PaidUntil = periodEnd` qo'yiladi (agar mavjud
`PaidUntil` dan kechroq bo'lsa). Tenant `Suspended` va sababi `NonPayment` bo'lsa — **avtomatik
`Active` ga qaytariladi**. `ITenantStateService` cache'i darhol tozalanadi.

`DELETE` — yozuvni soft-delete qiladi va `PaidUntil` ni qolgan yozuvlarning eng kechki
`periodEnd` iga qayta hisoblaydi (yoki `null`).

**Mijoz uchun:** `GET /api/subscription/me` javobiga qo'shing (to'liq shakl §Shartnoma da):
`paidUntil`, `daysUntilPaidEnd`, `paymentGraceDays`.

### Audit

`POST/DELETE .../payments` — `IsPlatformAction = true`, yozuv **maqsad tenantga** tushsin
(mijoz o'z izida to'lov qayd etilganini ko'rsin).

## Qabul mezoni

- To'lov qayd etilgach `PaidUntil` yangilanadi, `NonPayment` sababli suspend bo'lgan tenant o'ziga keladi.
- `PaidUntil` ni o'tmishga qo'yish → grace tugagach har so'rov **402 `payment_expired`**.
- `PaidUntil = null` bo'lgan tenant hech qachon bloklanmaydi.
- Fon xizmati muddati o'tgan tenantni `Suspended` ga o'tkazadi va ma'lumotga tegmaydi.
- `GET /api/admin/tenants/expiring?days=7` faqat 7 kun ichida tugaydiganlarni qaytaradi.

## Tegmaslik kerak

Trial mantiqiga (`TrialEndsAt`, `TrialDays`) — u o'z holicha qoladi va **demo** uchun ishlatiladi.

---

# 🔴 S2 — Vaqtincha to'xtatish, sabab va avtomatik qayta yoqish

## Maqsad

"Suspend" hozir bitta ko'r-ko'rona holat. Sizga sabab (to'lov / mijoz so'radi / texnik ish) va
**muddatli** to'xtatish kerak — belgilangan sanada o'zi qayta yoqilsin.

## Bajariladigan ishlar

### Domen

`Tenant` ga qo'shing:

| Maydon | Tur | Izoh |
|---|---|---|
| `SuspendReason` | enum? | `NonPayment`, `ClientRequest`, `Technical`, `Violation`, `Other` |
| `SuspendNote` | string? | Ichki izoh (mijozga **ko'rsatilmaydi**) |
| `SuspendPublicMessage` | string? | Mijozga ko'rsatiladigan matn (ixtiyoriy) |
| `SuspendedUntil` | DateTime? | Shu sanada avtomatik `Active` ga qaytadi |
| `SuspendedAt` | DateTime? | |
| `SuspendedByUserId` | int? | |

### API o'zgarishi

`PUT /api/admin/tenants/{id}/suspend` endi tana qabul qiladi:

```json
{
  "reason": "ClientRequest",
  "note": "Mijoz 2 oyga to'xtatishni so'radi",
  "publicMessage": "Tizim vaqtincha to'xtatilgan. Savollar uchun bog'laning.",
  "until": "2026-10-01T00:00:00Z"
}
```

`until` null bo'lsa — muddatsiz (hozirgi xatti-harakat).
`PUT .../activate` — barcha suspend maydonlarini tozalasin.

### Majburlash

- `SubscriptionPolicy` blok kodini sababga qarab qaytarsin:
  `suspended_nonpayment`, `suspended_request`, `suspended_technical`, `suspended_violation`, `suspended_other`.
- Fon xizmati `SuspendedUntil <= now` bo'lgan tenantlarni **avtomatik `Active`** ga qaytarsin
  (agar `PaidUntil` ham o'tgan bo'lmasa — u holda `NonPayment` bilan suspend qolsin).
- `GET /api/subscription/me` javobida `blockedReason`, `blockedMessage` (public), `suspendedUntil`.
  `SuspendNote` **hech qachon** mijoz javobiga chiqmasin.

## Qabul mezoni

- Sabab bilan suspend → mijoz 402 va aynan o'sha sababga mos kod oladi.
- `until` bilan suspend → o'sha sanadan keyingi birinchi fon ishlashida avtomatik yoqiladi.
- `SuspendNote` mijozning hech qaysi endpointida ko'rinmaydi (buni aniq tekshiring).
- Avtomatik yoqilish `PaidUntil` o'tgan tenantni yoqib yubormaydi.

---

# 🔴 S3 — Self-service registratsiyani yopish, demo so'rovi (lead)

## Maqsad

Hozir istalgan odam `/api/auth/register` orqali 14 kunlik to'liq trial oladi. Yangi modelda tenantni
faqat siz yaratasiz. Register o'rniga — "demo so'rash" formasi va admin paneldagi lead ro'yxati.

Bu bajarilgach **T11 / R8 (SMS tasdiqlash, CAPTCHA) reja'dan chiqadi** — tashqi SMS provayder kerak emas.

## Bajariladigan ishlar

### Registratsiyani yopish

- `Registration:SelfServiceEnabled` konfiguratsiya kaliti, **default `false`**.
- `false` bo'lsa `POST /api/auth/register` → `404` (endpoint borligini ham oshkor qilmaslik uchun).
- Kodni **o'chirmang** — `TenantProvisioner` va register mantiqi lead konversiyasida qayta ishlatiladi.

### Lead entity (platforma darajasida, `TenantId` yo'q)

`CompanyName`, `ContactName`, `Phone`, `Email?`, `Note?`,
`Source` (enum: `Website`, `Portal`, `Manual`, `Referral`),
`ReferrerTenantId?` (S7 uchun — kim orqali kelgani),
`Status` (enum: `New`, `Contacted`, `DemoGiven`, `Won`, `Lost`),
`StatusNote?`, `ConvertedTenantId?`, `CreatedAt`, `UpdatedAt`.

### API

**Public (autentifikatsiyasiz):**

```
POST /api/leads     demo so'rovi
```

Rate limit: IP bo'yicha **5/soat** (yangi `leads` policy). Mavjud `auth` policy'ni ishlatmang.
Bir xil telefon raqami 24 soat ichida qayta yuborilsa — yangi yozuv yaratmasdan `200` qaytaring
(spam yig'ilmasin, foydalanuvchi xato ko'rmasin).

**Admin (SuperAdmin):**

```
GET  /api/admin/leads?status=&source=&search=&page=&pageSize=
GET  /api/admin/leads/{id}
PUT  /api/admin/leads/{id}                status, statusNote
POST /api/admin/leads/{id}/convert        tenant yaratadi
```

`convert` tanasi — tenant yaratish DTO'si (nom, slug, plan, admin login/parol, `paidUntil?`).
Mavjud `TenantProvisioner` chaqiriladi. Muvaffaqiyatda lead `Won` bo'ladi va `ConvertedTenantId`
to'ldiriladi. Slug band bo'lsa — **400** va tushunarli xabar, lead o'zgarmaydi.

## Qabul mezoni

- `SelfServiceEnabled=false` da `POST /api/auth/register` → 404.
- `POST /api/leads` ishlaydi, 6-so'rov o'sha IP dan 429 oladi.
- Bir xil telefon 24 soat ichida takrorlansa dublikat yozuv yaratilmaydi.
- `convert` haqiqiy ishlaydigan tenant yaratadi (admin login qila oladi).

---

# 🔴 S4 — `TenantFeature`: menyu darajasidagi boshqaruv

## Maqsad

Hozirgi eng mayda dona — "modul" (11 ta). Sizga menyu darajasi kerak: A mijozga bitta sahifani
ochish yoki yopish. Uch qatlam bo'ladi:

| Qatlam | Savol | Kim boshqaradi | Holati |
|---|---|---|---|
| Plan | Qaysi to'plam sotildi? | SuperAdmin | ✅ bor |
| **Feature** | Bu tenantda bu imkoniyat **bormi**? | SuperAdmin | ← shu vazifa |
| Permission | Tenant ichida **kim** ishlatadi? | Mijozning admini | ✅ bor |

## Bajariladigan ishlar

### Domen

**`Feature`** — platforma katalogi (global, `TenantId` yo'q):
`Code` (unique, masalan `production.recipes`), `Name`, `Description?`,
`ModuleCode` (qaysi modulga tegishli — guruhlash uchun),
`DefaultEnabled` (bool), `IsCustom` (bool, S5 uchun), `SortOrder`.

**`Plan.FeatureCodes`** — CSV, mavjud `ModuleCodes` bilan bir xil uslubda (yangi jadval kerak emas).

**`TenantFeature`** — override: `TenantId`, `FeatureCode`, `IsEnabled`, `Note?`, `SetByUserId`, `SetAt`.

### Yechim tartibi (aniq shu ketma-ketlikda)

1. `TenantFeature` yozuvi bor → **o'sha** (plan ustidan yozadi).
2. Yo'q → tenantning planidagi `FeatureCodes` da bormi.
3. Plan yo'q → `Feature.DefaultEnabled`.
4. Feature tegishli modul o'chiq bo'lsa → **har doim o'chiq** (modul yuqori qatlam, feature uni yenga olmaydi).

### Majburlash

- `ITenantStateService` feature to'plamini modul to'plami bilan **bir joyda** cache qilsin
  (qo'shimcha DB o'qish bo'lmasin). Yozuvda cache darhol tozalansin.
- Yangi `RequireFeatureAttribute` — `RequireModuleAttribute` bilan aynan bir xil uslubda.
  Bir nechta kod berilsa "biror biri" semantikasi. Xato: **403** + `code: "feature_disabled:CODE"`.
- SuperAdmin mustasno.

### Boshlang'ich katalog (seed)

`DataInitializer` da ~25 ta feature seed qiling. Har biri mavjud bir sahifaga mos kelsin.
Taxminiy ro'yxat (kodni o'qib aniqlashtiring, sahifasi yo'q feature yozmang):

```
warehouse.locations        warehouse.stock-count      warehouse.batches
transfers.incoming         transfers.outgoing         transfers.internal
production.orders          production.recipes         production.stages
production.waste
qc.checks                  qc.parameters
finance.transactions       finance.debts              finance.cashflow
counterparties.suppliers   counterparties.clients     counterparties.portal
kpi.shifts                 kpi.attendance             kpi.efficiency
analytics.basic            analytics.advanced
export.excel               export.pdf                 import.excel
```

**Hammasi `DefaultEnabled = true`** va to'rt planning `FeatureCodes` iga o'z modullariga mos ravishda
to'ldirilsin — ya'ni **xatti-harakat bugungi holatdan o'zgarmasin**. Bu vazifa imkoniyat qo'shadi,
hech kimdan hech narsani tortib olmaydi.

### API

```
GET  /api/admin/features                      katalog (modul bo'yicha guruhlangan)
GET  /api/admin/tenants/{id}/features         yechilgan holat + manbasi
PUT  /api/admin/tenants/{id}/features         override qo'yish/olib tashlash
```

`GET .../tenants/{id}/features` har feature uchun: `code`, `name`, `moduleCode`, `isEnabled`,
`source` (`"tenant"` | `"plan"` | `"default"`), `note`. Frontend shu `source` ni ko'rsatadi.

`PUT` tanasi — `{ "features": [{ "code": "...", "isEnabled": true, "note": "..." }] }`.
`isEnabled` `null` yuborilsa — override **o'chiriladi** (planga qaytadi).

**Mijoz uchun:** login javobi va `GET /api/subscription/me` ga `enabledFeatures` (kodlar massivi).

## Qabul mezoni

- Seed'dan keyin hech bir mijozning menyusi o'zgarmaydi (regressiya yo'q).
- Feature'ni tenant darajasida o'chirish → o'sha endpoint 403 `feature_disabled:CODE`.
- Plandagi to'plamdan tashqari feature'ni yoqish ishlaydi va `source: "tenant"` ko'rinadi.
- Modul o'chirilsa — uning barcha feature'lari o'chiq (override bo'lsa ham).
- Override olib tashlansa plan qiymatiga qaytadi.

---

# 🟠 S5 — Maxsus (custom) feature konvensiyasi

## Maqsad

C mijozga faqat unga kerak bo'lgan fitcha yozish — **alohida branch va alohida deploysiz**.
Birinchi maxsus fitcha har doim "kichkina istisno" bo'lib ko'rinadi; beshinchisida boshqarib
bo'lmaydigan besh istisno bo'ladi. Konvensiyani birinchi so'rovdan **oldin** o'rnatamiz.

## Bajariladigan ishlar

### Domen

`Feature` ga qo'shing: `OwnerTenantId?` (kim uchun yozilgan), `RequestedAt?`, `Reason?`.

**Qoida:** `IsCustom = true` bo'lgan feature `DefaultEnabled = false` bo'lishi **shart** —
validatsiyada majburlang. Custom feature hech qanday planning `FeatureCodes` iga kirmaydi;
faqat `TenantFeature` override orqali yoqiladi. Buni ham validatsiyada tekshiring.

### Kod tashkiloti

```
WMS.Application/Features/Custom/<feature-code>/     mantiq
WMS.API/Controllers/Custom/                          controllerlar
```

Har custom controller **majburiy** `[RequireFeature("custom.<code>")]` bilan belgilansin.
Umumiy servislarga custom mantiq yozilmasin — umumiy kod ifloslanmasin.

### Reestr

`CUSTOM_FEATURES.md` yarating. Har yozuv: kod, qaysi mijoz, sana, sabab, so'ragan odam,
qaysi fayllar, **"uchinchi mijoz so'rasa umumiyga ko'chiriladi"** belgisi.
Ikki yildan keyin hech kim o'chirishga jur'at etmasligi shundan boshlanadi — hujjatsiz qoldirmang.

### API

`GET /api/admin/features?isCustom=true` — filtr qo'shing.

## Qabul mezoni

- `IsCustom = true` + `DefaultEnabled = true` yaratishga urinish → validatsiya xatosi.
- Custom feature planga qo'shishga urinish → validatsiya xatosi.
- `CUSTOM_FEATURES.md` mavjud va bo'sh shablon bilan.

## Eslatma

Bu vazifada **hech qanday haqiqiy custom fitcha yozilmaydi** — faqat skelet va qoidalar.

---

# 🟠 S6 — `Organization`: platforma darajasidagi kompaniya identifikatori

## Maqsad

Bugun `Counterparty` — bu **A ning ichki yozuvi**. Real kompaniya B tizimda mustaqil mavjud emas.
B keyinchalik o'zi tenant bo'lsa, ikkita bog'lanmagan B paydo bo'ladi.

Yechim: kompaniya identifikatorini tenantdan **yuqoriga** ko'tarish. Bu vazifa faqat identifikatorni
qo'yadi — hujjat almashinuvi va hamkorlik keyingi bosqichda (S8/S9).

> **Bu vazifani kechiktirmang.** Bugun STIR maydonini qo'shish — bir kunlik ish.
> Ming counterparty yozuvi to'plangandan keyin orqaga qarab bog'lash oylik loyihaga aylanadi,
> va ba'zi yozuvlarni umuman aniqlab bo'lmaydi.

## Bajariladigan ishlar

### Domen

**`Organization`** — global, `TenantId` **yo'q**, global soft-delete filtridan tashqarida emas
lekin tenant filtriga bog'liq emas:
`Name`, `Inn` (STIR — **unique, nullable**), `Phone?`, `Address?`, `CreatedAt`.

`Tenant.OrganizationId` (nullable FK) · `Counterparty.OrganizationId` (nullable FK).
`Counterparty` da `Inn` maydoni bo'lmasa — qo'shing.

### Bog'lash mantiqi

Bitta joyda: `IOrganizationResolver.ResolveAsync(inn, name, phone)` →
INN bo'yicha mavjudini topadi, yo'q bo'lsa yaratadi, `Organization` qaytaradi.

Chaqiriladigan joylar: counterparty yaratish/tahrirlash, tenant yaratish (`TenantProvisioner`),
counterparty Excel importi.

**INN validatsiyasi:** O'zbekiston STIR — **9 raqam**. Bo'sh bo'lishi mumkin (majburiy emas),
lekin kiritilsa formatga mos bo'lsin.

### Maxfiylik — muhim

Tenantlar `Organization` ro'yxatini **qidira olmasligi** kerak. Faqat aniq INN bo'yicha moslik
ishlaydi va u ham faqat ichki bog'lash uchun. Hech qanday `GET /api/organizations` tenant
endpointi bo'lmasin. Aks holda mijozlaringiz bir-birining mijozlar bazasini yig'ib oladi.

### Migration / backfill

Mavjud counterparty'larda INN bo'lsa — migration ichida `Organization` yaratib bog'lang.
Bir xil INN li bir necha yozuv → **bitta** Organization. INN yo'q yozuvlar `null` bo'lib qoladi
(keyin qo'lda to'ldiriladi).

### API (faqat SuperAdmin)

```
GET /api/admin/organizations?search=&page=&pageSize=
GET /api/admin/organizations/{id}      bog'langan tenantlar + counterparty'lar (qaysi tenantda)
```

## Qabul mezoni

- Bir xil INN bilan ikki tenantda counterparty yaratilsa → **bitta** `Organization` ga bog'lanadi.
- Noto'g'ri formatdagi INN → 400.
- INN siz counterparty yaratish ishlaydi (majburiy emas).
- Tenant tokeni bilan organization endpointlariga kirish mumkin emas.
- Migration mavjud bazada muammosiz o'tadi, dublikat Organization yaratmaydi.

---

# 🟠 S7 — Portal foydalanuvchisini to'liq versiyaga sotish

## Maqsad

A ning portalidagi ta'minotchi/xaridorlar — tayyor, tizimni ko'rgan, ishonchli lead'lar.
Ular uchun "qiziqaman" tugmasi qo'yamiz. **S3 tugagan bo'lishi shart** (`Lead` entity kerak).

## Bajariladigan ishlar

### API

```
POST /api/portal/upgrade-interest         counterparty portal tokeni bilan
POST /api/agent-portal/upgrade-interest   agent portal tokeni bilan
GET  /api/portal/upgrade-interest         yuborilganmi (banner holati uchun)
```

`POST` tanasi: `{ "phone": "...", "note": "..." }` — ism va kompaniya nomi tokendan olinadi.

Yaratiladigan `Lead`: `Source = Portal`, `ReferrerTenantId` = portal egasi tenant,
`CompanyName` = counterparty nomi, `Note` ga counterparty id va tenant nomi yoziladi.

**Muhim:** bu endpointlar `SubscriptionEnforcementMiddleware` dan **ozod** bo'lmasin —
A suspend bo'lsa portal ham to'xtaydi, bu to'g'ri xatti-harakat.

**Takroriy yuborish:** bir counterparty 30 kun ichida qayta yuborsa yangi lead yaratilmasin,
`GET` mavjud so'rov holatini qaytarsin.

`ReferrerTenantId` kelajakda A ga komissiya/bonus berish uchun asos — hozir faqat saqlanadi.

## Qabul mezoni

- Portal tokeni bilan so'rov yuborilsa lead paydo bo'ladi va `wms-admin` ro'yxatida ko'rinadi.
- `ReferrerTenantId` to'g'ri to'ldiriladi.
- 30 kun ichida takroriy so'rov dublikat yaratmaydi.
- Asosiy API tokeni bilan bu endpointga kirish mumkin emas (403).

---

# API shartnomasi — frontend shu shaklga tayanadi

> Bu bo'lim `TASKS_FRONTEND_S.md` da **aynan takrorlangan**. O'zgartirsangiz — ikkalasida.

## `GET /api/subscription/me` (S1, S2, S4 dan keyingi to'liq shakl)

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

`isBlocked = true` bo'lganda ham bu endpoint **200** qaytaradi — mijoz sababni ko'rishi shart.

## Xato kodlari (`ApiResponse.code`)

| HTTP | `code` | Ma'nosi |
|---|---|---|
| 402 | `trial_expired` | Demo muddati tugagan |
| 402 | `payment_expired` | To'lov muddati tugagan (S1) |
| 402 | `suspended_nonpayment` | To'lov sababli to'xtatilgan (S2) |
| 402 | `suspended_request` | Mijoz so'rovi bilan to'xtatilgan (S2) |
| 402 | `suspended_technical` | Texnik ish (S2) |
| 402 | `suspended_violation` | Qoida buzilishi (S2) |
| 402 | `suspended_other` | Boshqa sabab (S2) |
| 402 | `tenant_inactive` | Tenant o'chirilgan/faolsiz |
| 402 | `limit_users` / `limit_warehouses` / `limit_transfers` | Plan limiti |
| 403 | `module_disabled:CODE` | Modul yoqilmagan |
| 403 | `feature_disabled:CODE` | Feature yoqilmagan (S4) |

## Admin endpointlari — to'liq ro'yxat (yangi)

```
POST   /api/admin/tenants/{id}/payments
GET    /api/admin/tenants/{id}/payments
DELETE /api/admin/payments/{paymentId}
GET    /api/admin/tenants/expiring?days=7

PUT    /api/admin/tenants/{id}/suspend        (tana bilan: reason, note, publicMessage, until)

GET    /api/admin/leads
GET    /api/admin/leads/{id}
PUT    /api/admin/leads/{id}
POST   /api/admin/leads/{id}/convert

GET    /api/admin/features
GET    /api/admin/tenants/{id}/features
PUT    /api/admin/tenants/{id}/features

GET    /api/admin/organizations
GET    /api/admin/organizations/{id}
```

## Public

```
POST /api/leads                      demo so'rovi (rate limit 5/soat/IP)
POST /api/portal/upgrade-interest    portal tokeni bilan
GET  /api/portal/upgrade-interest
```

---

# Yakuniy tekshiruv (barcha S vazifalardan keyin)

1. `dotnet build WMS.sln` → 0 xato, 0 ogohlantirish.
2. Mavjud `wms.db` nusxasida migration — muammosiz.
3. Regressiya: eski mijoz (plansiz, `PaidUntil = null`) hech qanday joyda bloklanmaydi.
4. Regressiya: planli mijozning menyusi S4 seed'dan keyin **o'zgarmagan**.
5. Uchma-uch: to'lov qayd et → suspend → avtomatik qayta yoqilish → feature yoq/o'chir → lead → convert.
6. `BACKEND_SAAS_REPORT.md` ga yangi bo'lim qo'shing; `SAAS_STATUS.md` ni yangilang.
