# Frontend vazifalari — F1–F6 (qolgan ishlar)

> **Terminal:** frontend (`wms-ui` + `wms-admin`) · **Branch:** `saas-admin`
> **Sana:** 2026-08-05 · **Juftlik fayli:** `TASKS_BACKEND_B.md`
> **Asos:** 2026-08-05 kod auditi — F1–F8 (oldingi to'plam) bajarilgan va tasdiqlangan.

---

## 0. Kontekst

Audit natijasi: ikkala ilova prod build **0 xato**, `wms-ui` i18n 610 kalit × 4 fayl farqsiz,
`wms-admin` i18n 266 kalit × 3 fayl farqsiz. Obuna qatlami, feature guard'lari, to'lov,
lead, organization UI — hammasi joyida.

Qolgan oltita ish:

| # | Ish | Nega |
|---|---|---|
| **F1** | Aloqa ma'lumoti | 🔴 **Sotuvni to'sadigan yagona narsa** — 5 daqiqa |
| **F2** | Brendlash UI va qo'llash | Kelishilgan, boshlanmagan |
| **F3** | `isPlatformAction` ko'rsatish | Mijoz kim uni suspend qilganini bilmaydi |
| **F4** | Limit 80 % ogohlantirishi | Mijoz faqat 402 da to'xtaydi |
| **F5** | `expiring` endpointiga o'tish | Dashboard hamma tenantni yuklaydi |
| **F6** | `FRONTEND.md` ni sinxronlash | Hujjat koddan orqada |

**F1 ni birinchi qiling** — u boshqa hech narsaga bog'liq emas va eng qimmat xatoni yopadi.
F2 backend B1 ga bog'liq; F4 backend B2 ga.

### Umumiy qoidalar

`FRONTEND.md` §4 konventsiyalari: `inject()`, `standalone`, `OnPush`, faqat signals,
HTTP faqat `ApiService`, toast faqat `NotificationService`, sana uchun `date.util.ts`.
`wms-ui` da har matn **4 ta** i18n fayliga, `wms-admin` da **3 ta** ga.
Har vazifadan keyin `ng build --configuration production` → **0 xato**.

---

# 🔴 F1 — Aloqa ma'lumoti (R1)

`wms-ui/src/environments/environment.ts` va `environment.prod.ts`:

```
supportPhone: '+998 90 000 00 00'   ← placeholder
supportEmail: 'support@wms.uz'      ← placeholder
```

Bloklangan mijoz aynan shu raqamni ko'radi va unga qo'ng'iroq qiladi.

**Ish:** ikkalasini haqiqiysiga almashtiring. Ikkala faylda ham — `environment.ts` lokal
ishlashda ishlatiladi va sinov paytida siz ham xuddi shuni ko'rasiz.

**Qabul mezoni:** Obuna sahifasida, blok bannerida va login sahifasidagi qizil panelda
haqiqiy raqam chiqadi. Kod bazasida `90 000 00 00` qidiruvi natija bermaydi.

---

# 🟠 F2 — Brendlash: admin formasi va mijozda qo'llash

> Backend `B1` tugaganidan keyin. Shartnoma: `TASKS_BACKEND_B.md` §B1.

## Maqsad

Har mijoz o'z logosi va rangini ko'rsin — **yagona build**ni saqlagan holda.
Brendlash ma'lumot, kod emas.

## `wms-admin` — kiritish

Tenant yaratish/tahrirlash formasiga yangi **"Branding"** bo'limi:

| Maydon | UI |
|---|---|
| Keng logo | Fayl yuklash + jonli ko'rinish + o'chirish tugmasi |
| Kvadrat logo | Xuddi shunday |
| Asosiy rang | Rang tanlash (`p-colorpicker`) + hex kiritish maydoni |

**Klient tomonda validatsiya** (backend baribir tekshiradi, lekin foydalanuvchi darhol bilsin):
SVG/PNG/WebP · maksimal 512 KB · keng 600×200, kvadrat 512×512.

**Kontrast tekshiruvi — muhim.** Tanlangan rang oq matn bilan yetarli kontrast bermasa
(WCAG AA, nisbat < 4.5) ogohlantirish ko'rsating: *"Bu rangda matn yaxshi o'qilmaydi"*.
Bloklamang, lekin aytib qo'ying — aks holda mijoz och sariq tanlaydi va bu sizga qaytadi.

Yonida jonli ko'rinish: kichik sidebar maketi tanlangan logo va rang bilan.

Tenants jadvalida nom yonida kvadrat logo (bo'lsa) — ro'yxatni o'qishni osonlashtiradi.

## `wms-ui` — qo'llash

### Manba

Brendlash login javobida va `GET /api/subscription/me` da `branding` obyekti sifatida keladi.
Yangi servis emas — mavjud `SubscriptionService` (yoki `TenantService`) ichida signal qilib saqlang.

### Rang qo'llash

Bitta asosiy rangdan palitra hosil qiling. **Mijozdan bir nechta rang so'ramang.**

- CSS o'zgaruvchisi `--brand-primary` va undan hosil qilingan soyalar (hover, active, disabled,
  yengil fon) `document.documentElement` ga ishlash paytida qo'yiladi.
- PrimeNG Aura primary palitrasi shu qiymatlar bilan override qilinsin.
- Dark mode'da ham tekshiring — `:host-context(.dark-mode)` mavjud uslubi buzilmasin.
- `brandColor` `null` bo'lsa — `styles.scss` dagi standart pistachio palitrasi.

### Logo qo'llash

| Joy | Qaysi logo |
|---|---|
| Sidebar (ochiq) | `logoUrl`, yo'q bo'lsa tenant nomi matn sifatida |
| Sidebar (yig'ilgan, 72px) | `logoSquareUrl`, yo'q bo'lsa `logoUrl`, u ham yo'q bo'lsa bosh harf |
| Brauzer tab sarlavhasi | Tenant nomi |
| Favicon | `logoSquareUrl` (bo'lsa) |

**Login sahifasi hozircha standart qoladi** — bitta URL'da kim kirayotgani noma'lum.
Subdomenga o'tilganda `GET /api/public/branding?slug=` orqali ishlaydi; kodni shunga
tayyor qoldiring, lekin hozir chaqirmang.

### Miltillash muammosi

Brendlash `/subscription/me` javobi kelgunicha standart tema ko'rinadi, keyin sakraydi.
Oxirgi ma'lum brendlashni `localStorage` da saqlang va ilova ochilishida darhol qo'llang,
javob kelgach yangilang. Logout'da tozalang.

## Qabul mezoni

- Logo yuklangach mijoz sahifani yangilaganda ko'radi (cache eski faylni ushlab qolmaydi).
- Rang o'zgartirilgach butun UI moslashadi, dark mode buzilmaydi.
- Brendlashsiz tenantda hamma narsa avvalgidek.
- Sahifa ochilishida tema miltillamaydi.
- Logout → keyingi mijoz kirganda oldingi brendlash **qolmaydi**.

---

# 🟠 F3 — Platforma amallarini ko'rsatish (R5)

## Maqsad

Backend `AuditLogDto` da `isPlatformAction` va `ActorTenantId` bor, lekin hech qaysi ilovada
ko'rsatilmaydi. Mijoz o'z audit jurnalida kim uni suspend qilganini ko'rmaydi.

## `wms-ui` — audit sahifasi

`modules/settings/audit-log/`:

- `isPlatformAction` bo'lgan qatorlarga **"Platforma amali"** badge'i (mavjud `.badge-info` uslubi).
- Tooltip: qisqacha izoh — bu amalni platforma administratori bajargan.
- Filtr: "faqat platforma amallari".
- i18n 4 faylga.

## `wms-admin` — audit sahifasi umuman yo'q

Yangi sidebar bo'limi **Audit**:

- Jadval: sana · tenant · foydalanuvchi · amal · obyekt · `isPlatformAction` badge.
- Filtrlar: tenant · sana oralig'i · faqat platforma amallari.
- Tenant detail'dan havola: "shu tenantning audit jurnali".

Backend endpointini **Swagger** dan tasdiqlang — `/api/admin/audit` mavjudmi yoki mavjud
`/api/audit` ni SuperAdmin uchun kengaytirish kerakmi. Endpoint bo'lmasa **avval backendga
yozdiring**, taxminiy chaqiruv qilmang.

## Qabul mezoni

- Mijoz o'z jurnalida suspend yozuvini platforma amali sifatida ko'radi.
- `wms-admin` da barcha tenantlar bo'yicha audit ko'rinadi va tenant bo'yicha filtrlanadi.

---

# 🟠 F4 — Limit 80 % ogohlantirishi (R7)

> Backend `B2` tugaganidan keyin. Shartnoma: `TASKS_BACKEND_B.md` §B2.

## Maqsad

Hozir mijoz limitni faqat 402 bilan urilganda biladi.

## Ishlar

### Progress-bar hisobini backenddan oling

Obuna sahifasi hozir foizni o'zi hisoblaydi. Backend endi `usagePercent` va `isNearLimit`
qaytaradi — **o'shani ishlating**, o'zingiz hisoblamang. Ikki joyda hisob vaqt o'tib ajraladi.

### `warning` maydonini ishlang

Backend muvaffaqiyatli javobda ixtiyoriy `warning: { code, message }` qaytarishi mumkin.
Bu **xato emas** — HTTP 200/201 va `success: true`.

`ApiService` javob kelganda `warning` bo'lsa `NotificationService.warn()` chaqirsin —
markazlashgan joyda, har komponentda emas (mavjud "bitta toast" qoidasi).

Kodlar: `limit_warn_users` · `limit_warn_warehouses` · `limit_warn_transfers`.
Matn 4 tilga.

### Yaratish sahifalarida oldindan ogohlantirish

Foydalanuvchi, ombor va transfer yaratish formalarida — `isNearLimit` bo'lsa forma ustida
tinch ogohlantirish paneli: *"Plan limitiga yaqinlashdingiz: 20 / 25"*. Bloklamaydi.

## Qabul mezoni

- 80 % dan keyingi yaratish muvaffaqiyatli bo'ladi **va** ogohlantirish toast'i chiqadi.
- Ogohlantirish xato kabi ko'rinmaydi (rang, ikonka).
- Yaratish formalarida panel limitga yaqin bo'lgandagina chiqadi.
- Plansiz tenantda hech qanday ogohlantirish yo'q.

---

# 🟡 F5 — `expiring` endpointiga o'tish

`GET /api/admin/tenants/expiring?days=7` backendda mavjud, lekin ishlatilmaydi.
`wms-admin` dashboard `getTenants()` bilan **barcha** tenantni yuklab, `expiringSoon` va
`expired` ni client tomonda hisoblaydi.

50 mijozgacha muammo emas, lekin endpoint bekorga yozilgan va ro'yxat o'sib boradi.

**Ish:** dashboard kartalari `expiring` endpointidan foydalansin.
Tenants jadvalidagi filtrlar (`expiring` / `expired` / `nolimit`) hozirgidek qolsin —
u yerda baribir to'liq ro'yxat kerak.

**Qabul mezoni:** dashboard kartalari o'sha raqamlarni ko'rsatadi, "Expiring soon" bosilganda
filtrlangan jadvalga o'tish avvalgidek ishlaydi.

---

# 🟡 F6 — `FRONTEND.md` ni sinxronlash

`docs/FRONTEND.md` koddan orqada. Tuzatilishi kerak:

| Nima yozilgan | Aslida |
|---|---|
| §2.5 eski `SubscriptionInfo` shakli | `paidUntil`, `daysUntilPaidEnd`, `blockedMessage`, `suspendedUntil`, `enabledFeatures` bor |
| §2.6 xato jadvalida `subscription_suspended` | Endi `suspended_nonpayment` va yana to'rt variant, `payment_expired`, `feature_disabled` |
| §2.2 `auth: login · register (self-service)` | `register` endi demo so'rovi formasi (`request-demo` alias bilan) |
| §2.4 himoya qatlamlari — `featureGuard` yo'q | `feature.guard.ts` mavjud, 28 joyda ishlatiladi |
| §3.2 "`wms-admin` da Transloco yo'q" | **Noto'g'ri** — 3 til, `public/i18n/`, 266 kalit |
| §3.1 modullar ro'yxatida `leads`, `organizations` yo'q | Ikkalasi ham mavjud |
| §7 kamchiliklar | R16/R17/R18 bajarilgan; `supportPhone` F1 dan keyin tuzatiladi |

Qo'shing: `wms-ui` i18n endi **610** kalit (574 emas).

**Qoida:** hujjatga "bajarildi" deb yozishdan oldin koddan tasdiqlang. Bu loyihada
hujjat ikki marta yolg'on gapirgan — bir marta ishlamaydigan narsani "ishlaydi" deb,
bir marta ishlaydigan narsani "qilinmagan" deb. Ikkalasi ham keyingi sessiyani chalg'itadi.

---

# Ushbu vazifalar qamramaydigan narsalar

| Nima | Qachon |
|---|---|
| **Subdomen** (`ice-gold.warehouse-system.uz`) | Qaror qabul qilingan; birinchi 1–2 mijoz barqaror ishlagach. Avval DNS provayderda API borligini tekshiring |
| **S8/S9** hamkorlik va o'zaro savdo tarixi | Birinchi real juftlik paydo bo'lganda |
| **R6** muddat tugashi haqida xabar | 10 mijozdan oshganda; Telegram sotuvdan keyin |
| **R9** self-service plan o'zgartirish | Billing bilan birga |
| **R10** qattiq yozilgan toast matnlari | Kosmetik |
