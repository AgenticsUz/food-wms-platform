# Frontend vazifalari — F7–F12 (yakuniy yopish)

> **Terminal:** frontend (`wms-ui` + `wms-admin`) · **Branch:** `saas-admin`
> **Juftlik fayli:** `TASKS_BACKEND_B4-B7.md`
> **Asos:** 2026-08-05 kod auditi.

---

## 0. Kontekst

Backend B1–B3 va parol tiklash **bajarilgan**, frontend tomoni esa qilinmagan.
Ya'ni backendda qurilgan uchta narsa hozir umuman ishlatib bo'lmaydi:
brendlash, limit ogohlantirishi, parol tiklash.

| # | Ish | Holat |
|---|---|---|
| **F7** | Aloqa ma'lumoti | ❌ `+998 90 000 00 00` hali turibdi |
| **F8** | Parol tiklash UI | ❌ Backend tayyor, tugma yo'q |
| **F9** | Brendlash UI va qo'llash | ❌ `brandColor` / `logoUrl` frontendda umuman yo'q |
| **F10** | `wms-admin` audit sahifasi | ❌ **B4 ni kutadi** |
| **F11** | `warning` + `isNearLimit` | ⚠️ `isNearLimit` hali client tomonda hisoblanadi |
| **F12** | `expiring` endpointi | ❌ Dashboard hali hammasini yuklaydi |

**Tartib:** F7 → F8 → F9 → F11 → F12 → F10 (oxirgisi B4 tugagach).

### Qoidalar

`FRONTEND.md` §4: `inject()`, `standalone`, `OnPush`, faqat signals,
HTTP faqat `ApiService`, toast faqat `NotificationService`, sana uchun `date.util.ts`.
i18n: `wms-ui` → 4 fayl, `wms-admin` → 3 fayl (`public/i18n/`). Kalitlar soni **teng**.
Har vazifadan keyin `ng build --configuration production` → **0 xato**.

---

# 🔴 F7 — Aloqa ma'lumoti

`wms-ui/src/environments/environment.ts` va `environment.prod.ts` da
`supportPhone: '+998 90 000 00 00'` va `supportEmail: 'support@wms.uz'` — placeholder.

Bloklangan mijoz aynan shu raqamga qo'ng'iroq qiladi.

**Ish:** ikkala faylda ham haqiqiysiga almashtiring.

**Qabul mezoni:** kod bazasida `90 000 00 00` qidiruvi natija bermaydi.

---

# 🔴 F8 — Parol tiklash UI

## Backend shartnomasi (tayyor)

```
GET  /api/admin/tenants/{id}/users              → [{ id, login, fullName, isActive,
                                                     isSuperAdmin, roles[], createdAt, lastLoginAt }]
POST /api/admin/tenants/{id}/reset-user-password  { userId?, newPassword? }
POST /api/users/{id}/reset-password               { newPassword? }
     → { userId, login, fullName, newPassword }
```

`newPassword` javobda **faqat bir marta** keladi. Qayta olish imkoni yo'q.
Generatsiya: 12 belgi, chalkash belgilarsiz (`0`/`O`, `1`/`l`/`I` yo'q).
Qo'lda: minimal 8 belgi. Tiklashda foydalanuvchining barcha eski tokenlari darhol o'ladi.

## `wms-admin` — platforma darajasida

Tenants jadvali amal menyusiga **"Reset password"**.

**Dialog:**
- Foydalanuvchi tanlash (`p-select`) — `getTenantUsers` dan, ko'rinish: ism + login.
  Default: admin rolidagi foydalanuvchi. Faolsizlar ro'yxatda yorliq bilan ko'rinsin.
  `isSuperAdmin` bo'lganlar ko'rinsin, lekin ogohlantirish bilan.
- Rejim (radio): **"Generate automatically"** (default) / **"Set manually"**.
- Tasdiqlash matni: bu foydalanuvchining barcha faol sessiyalari bekor qilinadi va
  u qaytadan kirishi kerak — aniq yozilsin.

**Natija ekrani — eng muhim qism:**
- Dialog **yopilmasin**. Esc va tashqariga bosish ham yopmasin.
- Login va yangi parol katta, o'qiladigan shriftda (`.num` / JetBrains Mono uslubi).
- Har birida nusxalash tugmasi + "Copied" bildirishnomasi.
- Qizil ogohlantirish: bu parol boshqa ko'rsatilmaydi, hozir nusxa oling.
- Yopish tugmasi **"Done"**.
- **Parol toast'da chiqarilmasin** — toast yo'qoladi va tarixda qoladi.

## `wms-ui` — tenant ichida

Sozlamalar → Foydalanuvchilar sahifasida har qatorda **"Parolni tiklash"**.
`[hasPermission]="'users.edit'"` bilan.

Xuddi shu dialog mantiqi (soddaroq — foydalanuvchi allaqachon tanlangan).
Xuddi shu natija ekrani qoidalari.

**Chegaralar** (backend baribir rad etadi, lekin UI oldindan aytsin):
- O'ziga tiklash tugmasi **ko'rinmasin** — profil sahifasidagi "Parolni o'zgartirish" ga havola.
- SuperAdmin hisobida tugma ko'rinmasin.

## `MustChangePassword` (B6 tugagach)

Login javobida `mustChangePassword: true` bo'lsa — foydalanuvchi **darhol** parol
o'zgartirish ekraniga yo'naltirilsin. Boshqa sahifalarga o'tolmasin (guard bilan),
lekin `change-password` va `logout` ishlasin.

Backend bloklamaydi — majburlash to'liq frontendda.

## Qabul mezoni

- Dialog ochilganda admin foydalanuvchi oldindan tanlangan.
- Avtomatik rejimda parol maydoni ko'rinmaydi; qo'lda rejimda 8 belgidan qisqa yuborilmaydi.
- Natija ekranida nusxalash ishlaydi, dialog tasodifan yopilmaydi.
- Parol hech qanday toast yoki konsolda ko'rinmaydi.
- `wms-ui` da o'ziga va SuperAdmin'ga tugma ko'rinmaydi.
- `mustChangePassword` bo'lsa boshqa sahifaga o'tib bo'lmaydi.

---

# 🟠 F9 — Brendlash: admin formasi va mijozda qo'llash

## Backend shartnomasi (tayyor)

```
POST   /api/admin/tenants/{id}/logo?type=wide|square    multipart
DELETE /api/admin/tenants/{id}/logo?type=wide|square
GET    /api/public/branding?slug=                        (autentifikatsiyasiz)
```

`branding` obyekti login javobida va `/api/subscription/me` da:
`{ logoUrl, logoSquareUrl, brandColor }`. Chegara: 512 KB, SVG/PNG/WebP,
keng 600×200, kvadrat 512×512. Fayl nomida hash — cache buzilmaydi.

## `wms-admin` — kiritish

Tenant formasiga **"Branding"** bo'limi:

| Maydon | UI |
|---|---|
| Keng logo | Yuklash + jonli ko'rinish + o'chirish |
| Kvadrat logo | Xuddi shunday |
| Asosiy rang | `p-colorpicker` + hex kiritish maydoni |

Klient tomonda ham validatsiya (backend baribir tekshiradi, lekin foydalanuvchi darhol bilsin).

**Kontrast tekshiruvi — majburiy.** Tanlangan rang oq matn bilan WCAG AA (4.5) bermasa
ogohlantirish: *"Bu rangda matn yaxshi o'qilmaydi"*. Bloklamang, lekin aytib qo'ying —
aks holda mijoz och sariq tanlaydi va bu sizga qaytadi.

Yonida jonli ko'rinish: kichik sidebar maketi tanlangan logo va rang bilan.
Tenants jadvalida nom yonida kvadrat logo.

## `wms-ui` — qo'llash

### Rang

Bitta asosiy rangdan palitra hosil qiling — **mijozdan bir nechta rang so'ramang**.

- `--brand-primary` va undan hosil qilingan soyalar (hover, active, disabled, yengil fon)
  `document.documentElement` ga ishlash paytida qo'yiladi.
- PrimeNG Aura primary palitrasi shu qiymatlar bilan override qilinsin.
- Dark mode tekshirilsin — `:host-context(.dark-mode)` uslubi buzilmasin.
- `brandColor` `null` → standart pistachio palitrasi.

### Logo

| Joy | Qaysi |
|---|---|
| Sidebar (ochiq) | `logoUrl`, yo'q bo'lsa tenant nomi matn sifatida |
| Sidebar (yig'ilgan, 72px) | `logoSquareUrl` → `logoUrl` → bosh harf |
| Brauzer tab sarlavhasi | Tenant nomi |
| Favicon | `logoSquareUrl` (bo'lsa) |

**Login sahifasi hozircha standart qoladi** — bitta URL'da kim kirayotgani noma'lum.
`GET /api/public/branding?slug=` allaqachon tayyor va subdomenga o'tilganda ishga tushadi;
kodni shunga tayyor qoldiring, lekin hozir chaqirmang.

### Miltillash

Brendlash javob kelgunicha standart tema ko'rinadi, keyin sakraydi.
Oxirgi ma'lum brendlashni `localStorage` da saqlang, ilova ochilishida darhol qo'llang,
javob kelgach yangilang. **Logout'da tozalang** — keyingi mijoz oldingisining rangini ko'rmasin.

## Qabul mezoni

- Logo yuklangach mijoz sahifani yangilaganda ko'radi (cache eski faylni ushlamaydi).
- Rang o'zgartirilgach butun UI moslashadi, dark mode buzilmaydi.
- Brendlashsiz tenantda hamma narsa avvalgidek.
- Sahifa ochilishida tema miltillamaydi.
- Logout → keyingi mijozda oldingi brendlash qolmaydi.

---

# 🟠 F11 — Limit ogohlantirishi va hisobni backenddan olish

## Muammo

`subscription.model.ts` da `isNearLimit` **hali client tomonda** hisoblanadi
(`percent >= LIMIT_WARN_PERCENT`), holbuki B2 aynan shu hisobni backendga ko'chirgan.

Endi ikki joyda ikki xil hisob bor: `Subscription:LimitWarnPercent` ni backendda
o'zgartirsangiz frontend bilmaydi. Bu vaqt o'tib ajralib ketadigan turdagi nomuvofiqlik.

## Ishlar

### Hisobni olib tashlang

`usagePercent` va `isNearLimit` backenddan keladi — **o'shani ishlating**.
`LIMIT_WARN_PERCENT` konstantasini va client tomondagi hisobni **o'chiring**.
Backend `null` qaytarsa (plansiz tenant) — ogohlantirish yo'q.

### `ApiResponse.warning` ni ishlang

Backend muvaffaqiyatli javobda ixtiyoriy `warning: { code, message }` qaytaradi.
Bu **xato emas** — HTTP 200/201 va `success: true`.

`ApiService` javob kelganda `warning` bo'lsa `NotificationService.warn()` chaqirsin —
markazlashgan joyda, har komponentda emas (mavjud "bitta toast" qoidasi).

Kodlar: `limit_warn_users` · `limit_warn_warehouses` · `limit_warn_transfers` ·
`permissions_outside_plan` (B5 dan).

Ogohlantirish xato kabi ko'rinmasin — rang va ikonka farqli bo'lsin.

### Yaratish formalarida

Foydalanuvchi, ombor va transfer yaratish formalarida — `isNearLimit` bo'lsa forma ustida
tinch panel: *"Plan limitiga yaqinlashdingiz: 20 / 25"*. Bloklamaydi.

### Rollar sahifasi (B5 dan)

Permission ro'yxatida `isAvailable: false` bo'lganlar **yashirilmasin** — kulrang qilib,
*"Tarifingizga kirmaydi"* yorlig'i bilan ko'rsatilsin. Bu tushunmovchilikni yo'q qiladi
va yumshoq upsell bo'ladi.

## Qabul mezoni

- Client tomonda foiz hisobi qolmagan (kod bazasida qidirib tekshiring).
- 80 % dan keyingi yaratish muvaffaqiyatli **va** ogohlantirish toast'i chiqadi.
- Plansiz tenantda hech qanday ogohlantirish yo'q.
- Yopiq modul ruxsatlari kulrang ko'rinadi, biriktirish mumkin, `warning` chiqadi.

---

# 🟡 F12 — `expiring` endpointiga o'tish

`GET /api/admin/tenants/expiring?days=7` mavjud, lekin dashboard `getTenants()` bilan
**barcha** tenantni yuklab, `expiringSoon` va `expired` ni client tomonda hisoblaydi.

50 mijozgacha muammo emas, lekin ro'yxat o'sib boradi.

**Ish:** dashboard kartalari `expiring` endpointidan foydalansin.
Tenants jadvalidagi filtrlar hozirgidek qolsin — u yerda baribir to'liq ro'yxat kerak.

**Qabul mezoni:** kartalar o'sha raqamlarni ko'rsatadi, bosilganda filtrlangan
jadvalga o'tish avvalgidek ishlaydi.

---

# 🟠 F10 — `wms-admin` audit sahifasi

> **B4 tugagandan keyin.** Endpoint hozir mavjud emas — taxminiy chaqiruv yozmang.

## Backend shartnomasi (B4 dan keyin)

```
GET /api/admin/audit?tenantId=&userId=&entityType=&action=
                    &platformOnly=&from=&to=&page=&pageSize=
```

Javobda `tenantName` bor — har qator uchun alohida so'rov qilmang.

## Ishlar

Yangi sidebar bo'limi **Audit**:

- Jadval: sana · tenant · foydalanuvchi · amal · obyekt · `isPlatformAction` badge.
- Filtrlar: tenant (select) · sana oralig'i · amal turi · **faqat platforma amallari**.
- Server tomonda sahifalash (`p-table` lazy rejimi) — barcha yozuvni yuklamang.
- Tenant detail'dan havola: "shu tenantning audit jurnali" → filtr oldindan qo'yilgan holda.

`wms-ui` tomoni allaqachon bajarilgan (badge + `platformOnly` filtri) — tegmang.

## Qabul mezoni

- Barcha tenantlar bo'yicha audit ko'rinadi va tenant bo'yicha filtrlanadi.
- Sahifalash server tomonda ishlaydi.
- "Faqat platforma amallari" filtri to'g'ri ishlaydi.

---

# Yakuniy tekshiruv (barcha vazifalardan keyin)

1. `wms-ui` va `wms-admin` prod build → **0 xato**.
2. i18n: `wms-ui` 4 fayl, `wms-admin` 3 fayl — kalitlar soni teng (skript bilan).
3. `FRONTEND.md` yangilansin: brendlash qatlami, parol tiklash, audit sahifasi,
   `warning` ishlovi. §7 "kamchiliklar" jadvalidan bajarilganlarni olib tashlang.
4. **R2 — jonli uchma-uch sinov.** Bu hech qachon o'tkazilmagan va deploydan oldin
   majburiy. Ketma-ketlik:

   ```
   demo so'rovi → admin lead → convert → tenant yaratildi (logo + rang + STIR bilan)
   → mijoz login → brendlash qo'llandi
   → plan biriktirildi → menyu mos keldi
   → feature o'chirildi → menyu yo'qoldi → URL 403
   → 3-ombor yaratish → 402 limit
   → 80 % chegarasi → warning toast
   → to'lov qayd etildi → paidUntil yangilandi
   → suspend (sabab + mijozga xabar + until) → mijoz AYNAN o'sha matnni ko'rdi
   → until sanasi keldi → avtomatik qayta yoqildi
   → parol tiklandi → eski token 401 → yangi parol bilan kirdi → parol o'zgartirishga majbur
   → audit: barcha platforma amallari maqsad tenantda ko'rindi
   ```

Har qadamda **ikkala** ilovada natijani tekshiring — backend to'g'ri javob berishi
frontend to'g'ri ko'rsatishini anglatmaydi.
