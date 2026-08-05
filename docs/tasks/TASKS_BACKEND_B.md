# Backend vazifalari — B1–B3 (qolgan ishlar)

> **Terminal:** backend (`wms-api`) · **Branch:** `saas-admin`
> **Sana:** 2026-08-05 · **Juftlik fayli:** `TASKS_FRONTEND_B.md`
> **Asos:** 2026-08-05 kod auditi — S1–S7 to'liq bajarilgan va tasdiqlangan.

---

## 0. Kontekst

Kod auditi ko'rsatdi: backend S1–S7 va frontend F1–F8 **bajarilgan**, ikkala prod build 0 xato,
i18n paritesi joyida. Qolgan ishlar kam va aniq.

Backend tomonda uchta narsa:

| # | Ish | Nega |
|---|---|---|
| **B1** | Brendlash (logo, rang) | Kelishilgan, lekin **umuman boshlanmagan** — `Tenant` da maydon yo'q |
| **B2** | Limit 80 % ogohlantirishi | Hozir mijoz faqat 402 da to'xtaydi, oldindan bilmaydi |
| **B3** | Hujjatlarni kod bilan sinxronlash | `CLAUDE.md`/`BACKEND.md` koddan orqada |

### Umumiy qoidalar

- `dotnet build WMS.sln` → **0 xato, 0 ogohlantirish**.
- Javob har doim `ApiResponse<T>`, xato sababi `code` maydonida.
- `TenantId` faqat JWT'dan. Sana UTC. Pul `decimal`.
- Yangi cheklov qo'shsangiz — majburlashini **o'sha commitda**.
- Build gotcha: API jarayoni `bin/` ni qulflaydi → `dotnet build WMS.sln -p:OutDir="<temp>\" -v q`.

Tartib: **B1 → B2 → B3**. B1 va B2 bir-biriga bog'liq emas, xohlasangiz almashtiring.

---

# 🟠 B1 — Brendlash: logo, nom va asosiy rang

## Maqsad

Har mijoz o'z logosi va rangini ko'rsin. Bu **yagona build**ni saqlashning sharti:
brendlash **ma'lumot** bo'lishi kerak, kod emas. Aks holda birinchi mijozdayoq alohida
build qilishga majbur bo'lasiz.

**Chegara qat'iy:** logo + kvadrat logo + bitta asosiy rang. Boshqa hech narsa.
"Sidebar joylashuvi", "menyu tartibi", "modul rangi" kabi so'rovlar keladi — ular
brendlash emas, ular **kod** va ularga yo'l qo'yilmaydi.

## Domen

`Tenant` ga uchta maydon:

| Maydon | Tur | Izoh |
|---|---|---|
| `LogoUrl` | string? | Keng logo — sidebar ochiq holatda, login sahifasi, hisobot sarlavhasi |
| `LogoSquareUrl` | string? | Kvadrat/ikonka — sidebar yig'ilgan, favicon |
| `BrandColor` | string? | Hex (`#RRGGBB`). Bitta asosiy rang, palitra frontendda hosil qilinadi |

Uchalasi ham `null` bo'lishi mumkin → standart tema qo'llanadi.

## Fayl yuklash

```
POST   /api/admin/tenants/{id}/logo?type=wide|square    multipart/form-data
DELETE /api/admin/tenants/{id}/logo?type=wide|square
```

**Validatsiya (majburiy, aks holda 4 MB skanerlangan JPG keladi):**

| Qoida | Qiymat |
|---|---|
| Format | `image/svg+xml`, `image/png`, `image/webp` |
| Maksimal hajm | **512 KB** |
| Maksimal o'lchov | keng: 600×200 px · kvadrat: 512×512 px |
| SVG | Ichidagi `<script>` va tashqi havolalar **tozalansin** yoki fayl rad etilsin |

Saqlash: `wwwroot/uploads/tenants/{tenantId}/logo-wide.<ext>`. Fayl nomiga
qisqa hash qo'shing (`logo-wide-a3f9.png`) — brauzer cache'i eski logoni ushlab qolmasin.
Eski fayl almashtirilganda o'chirilsin.

Static serving `wwwroot` orqali — bu yo'l **autentifikatsiyasiz** ochiq bo'ladi
(logo maxfiy ma'lumot emas, va login sahifasida kerak bo'ladi).

`BrandColor` oddiy `PUT /api/admin/tenants/{id}` orqali. Validatsiya: qat'iy `#RRGGBB`.
Kontrast tekshiruvi frontendda — bu yerda faqat format.

## Brendlashni yetkazish

Uch joyda qaytarilsin, **yangi endpoint yaratmasdan**:

1. **Login javobida** — mijoz kirgan zahoti tema qo'llanadi.
2. **`GET /api/subscription/me`** — sahifa yangilanganda tiklanadi.
3. **`GET /api/public/branding?slug={slug}`** — autentifikatsiyasiz, **faqat** `name`,
   `logoUrl`, `logoSquareUrl`, `brandColor` qaytaradi.

Uchinchisi login sahifasi uchun. Hozir kerak emas (bitta URL — kim kirayotgani noma'lum),
lekin subdomenga o'tganda darhol ishlaydi. Rate limit qo'ying (IP bo'yicha 30/daqiqa) va
mavjud bo'lmagan slug uchun ham **200 + bo'sh brendlash** qaytaring — slug mavjudligini
oshkor qilmaslik uchun.

Barcha uchtasida shakl bir xil:

```json
"branding": {
  "logoUrl": "/uploads/tenants/3/logo-wide-a3f9.png",
  "logoSquareUrl": null,
  "brandColor": "#2E7D32"
}
```

## Hisobot sarlavhalari — unutilmaydigan qism

Mijoz hisobotni bosib chiqarib direktoriga beradi. U yerda o'z logosini ko'rmasa,
birinchi so'rov aynan shu bo'ladi.

- **PDF** (yuk xati, transfer) — sarlavhaga `LogoUrl` va tenant nomi.
- **Excel eksport** — birinchi qatorga logo va nom.

Logo yo'q bo'lsa faqat nom yozilsin, generator yiqilmasin.

## Qabul mezoni

- 512 KB dan katta fayl, noto'g'ri format, o'lcham chegarasidan oshgan rasm → **400** + tushunarli xabar.
- SVG ichida `<script>` bo'lsa → rad etiladi yoki tozalanadi.
- Logo almashtirilgach eski fayl diskda qolmaydi va URL o'zgaradi (cache buzilmaydi).
- `#ZZZZZZ` kabi noto'g'ri rang → 400.
- Login javobi, `/subscription/me` va public endpoint **bir xil** `branding` obyektini qaytaradi.
- Brendlashsiz tenantda hamma narsa avvalgidek ishlaydi (uchala maydon `null`).
- PDF va Excel logosiz ham xatosiz yaratiladi.

## Tegmaslik kerak

Tenant admin o'zi logo yuklay olmaydi — faqat SuperAdmin. Mijoz o'zi brendlaydigan
bo'lsa bu alohida qaror va alohida vazifa.

---

# 🟠 B2 — Limit 80 % ga yetganda ogohlantirish

## Maqsad

Hozir mijoz limitni faqat **402 bilan urilganda** biladi. Ombor yaratmoqchi bo'lgan odam
"nega ishlamayapti" deb sizga qo'ng'iroq qiladi. Oldindan aytish arzonroq.

`Subscription:LimitWarnPercent` (default 80) konfiguratsiyasi allaqachon bor — ishlatilmayapti.

## Ishlar

### `SubscriptionInfo.limits` ga qo'shing

Har limit uchun `usagePercent` (0–100, `decimal`) va `isNearLimit` (bool — `>= LimitWarnPercent`).
Limitsiz (plansiz) tenantda ikkalasi ham `null`.

Frontend hozir foizni o'zi hisoblayapti — hisob **bitta joyda**, backendda bo'lsin,
aks holda ikkovi vaqt o'tib ajralib ketadi.

### `ApiResponse` ga ogohlantirish kanali

Yangi ixtiyoriy maydon: `warning` (`{ code, message }`), `null` bo'lishi mumkin.

Muvaffaqiyatli yaratishdan keyin chegara **kesib o'tilgan** bo'lsa to'ldiring:

| Amal | `warning.code` |
|---|---|
| Foydalanuvchi yaratildi, foydalanish ≥ 80 % | `limit_warn_users` |
| Ombor yaratildi, ≥ 80 % | `limit_warn_warehouses` |
| Transfer yaratildi, ≥ 80 % | `limit_warn_transfers` |

Muhim: bu **xato emas**. HTTP **200/201** qoladi, `success: true` qoladi.
Faqat qo'shimcha maydon. Mavjud frontend kodini buzmaydi.

Xabar `Accept-Language` bo'yicha tarjima qilinsin (mavjud `ResponseLocalizationFilter` uslubida);
`code` tarjima qilinmaydi.

`PlanLimits` da hisob allaqachon bor — qayta yozmang, shu joydan foydalaning.

## Qabul mezoni

- 25 foydalanuvchilik planda 20-chi yaratilganda → 201 + `warning.code = limit_warn_users`.
- 19-chida `warning` **null**.
- Limitga yetganda avvalgidek **402 `limit_users`** (o'zgarmaydi).
- Plansiz tenantda hech qachon warning yo'q, `usagePercent` null.
- Mavjud endpointlarning javob shakli boshqa jihatdan o'zgarmagan.

---

# 🟡 B3 — Hujjatlarni kod bilan sinxronlash

## Maqsad

`docs/CLAUDE.md` R16/R17/R18 ni hali bajarilmagan deb ko'rsatadi — aslida bajarilgan.
Bu keyingi sessiyani chalg'itadi.

Bu loyihada bir marta teskari yo'nalishda bo'lgan: hujjat "ishlaydi" deb yozgan, ishlamagan.
Endi "qilinmagan" deb yozgan, qilingan. **Ikkalasi ham bir xil zarar.**

## Ishlar

### `docs/CLAUDE.md`

- §3 holat jadvalini yangilang: frontend F1–F8 bajarilgan.
- §4 dan **R16, R17, R18** ni olib tashlang (bajarilgan).
- **R1 qoladi** — `supportPhone`/`supportEmail` hali placeholder (frontend tomonda).
- **R5** (`isPlatformAction` UI) va **R7** (80 % ogohlantirish) qoladi; R7 ning backend
  qismi B2 da bajariladi.
- Yangi qatorlar: **B1 brendlash**, **subdomen** (qaror qabul qilingan, birinchi 1–2 mijozdan keyin),
  **S8/S9** hamkorlik (birinchi real juftlik paydo bo'lganda).
- §2 texnologiya jadvalidagi "i18n — faqat `wms-ui`" **noto'g'ri**: `wms-admin` da ham
  Transloco bor (3 til, `public/i18n/`, 266 kalit).

### `docs/BACKEND.md`

- B1 va B2 dan keyingi yangi endpointlar va DTO maydonlari.
- `ApiResponse.warning` maydoni va uning semantikasi (xato emas).
- Brendlash saqlash joyi va validatsiya qoidalari.

### Tekshirilgan holat (o'zgartirmang, faqat tasdiqlang)

2026-08-05 auditida quyidagilar kod darajasida tasdiqlangan:
S1–S7 to'liq · `wms-admin` prod build 0 xato · `wms-ui` prod build 0 xato ·
`wms-ui` i18n 610 kalit × 4 fayl farqsiz · `wms-admin` i18n 266 kalit × 3 fayl farqsiz.

### Kichik topilma

`GET /api/admin/tenants/expiring` endpointi mavjud, lekin frontend uni ishlatmaydi —
dashboard barcha tenantni yuklab, foizni client tomonda hisoblaydi. 50 mijozgacha muammo emas.
Frontend tomonda `TASKS_FRONTEND_B.md` F5 da hal qilinadi — endpointni **o'chirmang**.

## Qabul mezoni

Hujjatda "bajarildi" deb yozilgan har bir narsa kodda mavjud. Tekshirmasdan yozmang —
`CLAUDE.md` §7 dagi "Tekshirmasdan aytmang" qoidasi aynan shu uchun.
