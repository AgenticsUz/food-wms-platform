# Backend vazifalari — B4–B7 (yakuniy yopish)

> **Terminal:** backend (`wms-api`) · **Branch:** `saas-admin`
> **Juftlik fayli:** `TASKS_FRONTEND_F7-F12.md`
> **Asos:** 2026-08-05 kod auditi. B1–B3 va parol tiklash bajarilgan va tasdiqlangan.

---

## 0. Kontekst

Backend deyarli tayyor. To'rtta ish qoldi — uchtasi frontend uchun **to'siq**, bittasi hujjat.

| # | Ish | Nega |
|---|---|---|
| **B4** | `/api/admin/audit` endpointi | Yo'q. `wms-admin` da audit sahifasi qurib bo'lmaydi |
| **B5** | Permission katalogini filtrlash | Mijoz sotib olmagan modul ruxsatini rolga biriktira oladi → 403 → sizga qo'ng'iroq |
| **B6** | `MustChangePassword` bayrog'i | Tiklangan parol telefonda aytiladi va abadiy amal qiladi |
| **B7** | `CLAUDE.md` ichki ziddiyatlari | To'rtta joyda fayl o'zi bilan zid |

**Tartib:** B4 birinchi (frontend uni kutadi). Qolganlari istalgan tartibda.

### Qoidalar

- `dotnet build WMS.sln` → **0 xato, 0 ogohlantirish**.
  Build gotcha: API jarayoni `bin/` ni qulflaydi → `-p:OutDir="<temp>\" -v q`.
- Javob `ApiResponse<T>`, xato sababi `code` da. `TenantId` faqat JWT'dan. Sana UTC.
- Yangi cheklov qo'shsangiz — majburlashini o'sha commitda.

---

# 🔴 B4 — Platforma darajasidagi audit endpointi

## Muammo

`AuditController` da faqat ikkita endpoint bor (`GET /api/audit`, `GET /api/audit/entity-types`)
va ikkalasi ham JWT'dagi `TenantId` bilan chegaralangan. SuperAdmin barcha tenantlar bo'yicha
audit ko'ra olmaydi.

`AuditLog` da `IsPlatformAction` va `ActorTenantId` allaqachon bor (T10), lekin ularni
ko'rsatadigan joy yo'q.

## Ishlar

```
GET /api/admin/audit?tenantId=&userId=&entityType=&action=
                    &platformOnly=&from=&to=&page=&pageSize=
```

`[Authorize(Policy = "SuperAdmin")]`.

- `tenantId` berilmasa — **barcha** tenantlar.
- `platformOnly=true` — faqat `IsPlatformAction` yozuvlari.
- Sahifalash majburiy: default `pageSize=50`, maksimal `200`.
  Audit jadvali eng tez o'sadigan jadval — sahifalashsiz qoldirmang.
- Javobda tenant nomi ham bo'lsin (frontend har qator uchun alohida so'rov qilmasin).
- Mavjud `AuditLogDto` ni kengaytiring: `tenantName`, `actorTenantName` (bo'lsa).

Yangi `GET /api/admin/audit/tenants` — filtr uchun tenant ro'yxati (id + nom),
yoki mavjud `GET /api/admin/tenants` yetarli bo'lsa uni ishlating va shuni hisobotda yozing.

**Indeks:** `AuditLog` da `(TenantId, CreatedAt)` bo'yicha indeks bormi tekshiring.
Yo'q bo'lsa qo'shing — bu so'rov sahifalanadi va sanaga qarab tartiblanadi.

## Qabul mezoni

- `tenantId` siz chaqiruv barcha tenant yozuvlarini qaytaradi.
- `platformOnly=true` faqat platforma amallarini qaytaradi.
- Tenant tokeni bilan bu endpointga kirish → **403**.
- `pageSize=1000` yuborilsa 200 ga cheklanadi, xato emas.
- 10 000 yozuvli jadvalda so'rov sekinlashmaydi (indeks tekshirilgan).

---

# 🟠 B5 — Permission katalogini yoqilgan modul/feature bo'yicha filtrlash

## Muammo

`UserService.GetAllPermissionsAsync()` **barcha** permission'larni filtrsiz qaytaradi.
`AssignPermissionsAsync` ham filtrlamaydi.

Amalda shunday bo'ladi: Basic planli mijoz Rollar sahifasini ochadi, "Ishlab chiqarish"
ruxsatlarini ko'radi, texnologga biriktiradi, saqlaydi — tizim qabul qiladi. Texnolog
kiradi, menyuda hech narsa yo'q. Admin URL yozadi — 403. Sizga qo'ng'iroq qiladi:
*"ruxsat berdim, ishlamayapti"*.

Bu xavfsizlik teshigi **emas** — gate baribir 403 beradi. Bu sotuvdan keyingi
qo'llab-quvvatlash muammosi va u har yangi mijozda takrorlanadi.

## Ishlar

### Katalogni boyiting, kesmang

`GetAllPermissionsAsync` o'rniga tenant kontekstini oladigan variant:
har `PermissionDto` ga **`isAvailable`** (bool) qo'shing — permission tegishli modul
(va feature, agar bog'lash mumkin bo'lsa) tenantda yoqilganmi.

**Ro'yxatdan olib tashlamang.** Frontend ularni kulrang qilib, "Tarifingizga kirmaydi"
yorlig'i bilan ko'rsatadi — bu tushunmovchilikni yo'q qiladi **va** yumshoq upsell bo'ladi.

`Permission.Module` maydoni allaqachon bor. Uning qiymatlari `ModuleCodes` bilan
mos kelishini tekshiring — mos kelmasa mapping jadvali yozing va uni hisobotda ko'rsating.

### Biriktirishda

`AssignPermissionsAsync` da `isAvailable = false` bo'lgan permission kelsa —
**jimgina tashlab yubormang**, chunki admin nima saqlanganini bilmay qoladi.
Qabul qiling va saqlang (gate baribir bloklaydi), lekin javobda
`warning` qaytaring (B2 dagi `ApiResponse.Warning` mexanizmi):
`code: "permissions_outside_plan"`, xabarda nechtasi tarifga kirmasligi.

Sababi: mijoz planni keyin kengaytirsa, rol allaqachon to'g'ri sozlangan bo'ladi.

## Qabul mezoni

- Basic planli tenantda `production.*` ruxsatlari `isAvailable = false` bilan qaytadi.
- Ularni rolga biriktirish ishlaydi va `warning` qaytaradi.
- Yoqilgan modul ruxsatlarida `isAvailable = true`, `warning` yo'q.
- SuperAdmin uchun hamma narsa `true`.

---

# 🟠 B6 — `MustChangePassword`: tiklangan parol bir martalik bo'lsin

## Muammo

Parol tiklash ishlaydi, lekin generatsiya qilingan parol **abadiy amal qiladi**.
Siz uni telefonda yoki Telegram orqali aytasiz — u o'sha kanalda qolib ketadi.

## Ishlar

`User` ga `MustChangePassword` (bool, default `false`).

**`true` bo'ladigan joylar:**
- `PasswordResetService.ApplyAsync` — har ikkala tiklash yo'lida (platforma va tenant admin).
- `TenantProvisioner` — yangi tenant admin hisobi yaratilganda.
- `UserService.CreateAsync` — yangi xodim yaratilganda.

**`false` bo'ladigan yagona joy:** `AuthService.ChangePasswordAsync` — foydalanuvchi
o'z parolini o'zgartirganda.

### Majburlash

Login javobiga `mustChangePassword` maydoni qo'shilsin.

Backend **bloklamasin** — frontend foydalanuvchini parol o'zgartirish ekraniga
yo'naltiradi. Sababi: bloklash `change-password` endpointining o'zini ham
bloklab qo'yish xavfini tug'diradi va zanjirli muammo yaratadi.

Agar keyinchalik qattiq majburlash kerak bo'lsa — o'sha paytda alohida qaror.

## Qabul mezoni

- Parol tiklangach login javobida `mustChangePassword: true`.
- Foydalanuvchi parolni o'zgartirgach `false` bo'ladi va qaytmaydi.
- Mavjud foydalanuvchilarda migration'dan keyin `false` (hech kimning ishi to'xtamasin).

---

# 🟡 B7 — `CLAUDE.md` ichki ziddiyatlarini tuzatish

Fayl to'rt joyda o'zi bilan zid. Bu keyingi sessiyani chalg'itadi.

| Qator | Muammo | To'g'risi |
|---|---|---|
| 3 | "Oxirgi yangilanish: 2026-08-04" | §3 da 2026-08-05 — sarlavhani yangilang |
| 40 | Daraxtda `wms-admin` — "inglizcha" | Uch tilli (`uz`, `ru`, `en`, 266 kalit) — §2 texnologiya jadvali bilan zid |
| 107 | "Nima allaqachon tayyor" ro'yxatida **"Self-service registratsiya"** | S3 uni yopgan. Olib tashlang yoki "demo so'rovi (lead) oqimi" deb almashtiring |
| §3 B1–B3 jadvali | **Parol tiklash yo'q** | Faqat `BACKEND.md` da hujjatlashtirilgan. Kirish nuqtasidan ko'rinmaydi — jadvalga qator qo'shing |

Qo'shimcha: B4–B6 dan keyingi yangi endpointlar va maydonlar `BACKEND.md` ga,
§4 "Qolgan ishlar" jadvali yangilansin.

**Qoida:** "bajarildi" deb yozishdan oldin koddan tasdiqlang.
Bu loyihada hujjat ikki marta yolg'on gapirgan — bir marta ishlamaydigan narsani
"ishlaydi" deb, bir marta ishlaydigan narsani "qilinmagan" deb.
