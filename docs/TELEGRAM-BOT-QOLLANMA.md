# Telegram bot — foydalanuvchi qo'llanmasi

> Bu hujjat **foydalanuvchilar uchun**: kim nima qila oladi, qanday ulanadi, qaysi
> buyruq nima qaytaradi. Texnik topshiriq va qaror sabablari — `TELEGRAM-BOT-TZ.md`.
> Bot: **@AgenticsWmsBot** (bitta bot barcha tashkilotlar uchun).

---

## 1. Bot nima qiladi

Bot WMS'ning **qo'shimcha oynasi**: ombor, transfer, moliya va davomat ma'lumotlarini
Telegram orqali o'qish va ayrim amallarni (transfer tasdiqlash, yetkazishni belgilash)
bajarish imkonini beradi. Bot orqali yangi hujjat yaratib bo'lmaydi — u ko'rish,
tasdiqlash va xabar olish uchun.

Bir bot uch xil odam bilan ishlaydi. Kim ekaningiz **qanday ulanganingiz** bilan
aniqlanadi:

| Kim | Qanday ulanadi | Nimaga ega bo'ladi |
|---|---|---|
| **Xodim** (admin, menejer, ishchi, kuzatuvchi) | O'zi: WMS → Sozlamalar → Profil → «Telegram'ga ulash» | So'rov buyruqlari, davomat, hisobot, bildirishnomalar |
| **Haydovchi** | Menejer havola beradi (haydovchi kartochkasidan) | Bugungi marshrut, yetkazdim/yetkazmadim tugmalari, yuk xati PDF |
| **Mijoz (kontragent)** | Menejer havola beradi (kontragent kartochkasidan) | Buyurtma va yetkazish xabarlari, `/qarzim` |
| **Guruh chat** | Administrator guruhda `/ulash` yozadi | Umumiy bildirishnomalar (zaxira, transfer, ishlab chiqarish) |

---

## 2. Ulanish

### 2.1 Xodim (o'zingiz ulanasiz)

1. WMS'ga kiring → **Sozlamalar → Profil**.
2. **«Telegram'ga ulash»** tugmasini bosing — bir martalik havola chiqadi.
3. Havolani bosing, Telegram ochiladi, **Start** ni bosing.
4. Bot javob beradi: `✅ <Tashkilot> ga ulandi. Bildirishnomalar shu yerga keladi.`
   Profil sahifasi ham o'zi yangilanadi.

**Muhim:**
- Havola **10 daqiqa** amal qiladi va **bir marta** ishlaydi. Kechiksangiz — yangisini oling.
- Bir vaqtda faqat bitta tirik havola bo'ladi: yangisini olsangiz, eskisi bekor bo'ladi.
- Bir Telegram akkaunti **bir nechta tashkilotga** ulanishi mumkin.
- Boshqa Telegram akkauntidan qayta ulasangiz, xabarlar **faqat yangi chatga** keladi.

### 2.2 Haydovchi (menejer ulaydi)

1. Menejer: WMS → **Yetkazish → Haydovchilar** → haydovchi kartochkasi → Telegram havolasi.
   (Ruxsat: `delivery.manage`, tarifda `delivery.fleet` bo'lishi shart.)
2. Havolani haydovchiga yuboring (Telegram, SMS — farqi yo'q). Havola **24 soat** amal qiladi.
3. Haydovchi Start bosadi → `✅ <Tashkilot> haydovchisi sifatida ulandingiz.`

Uzish: o'sha kartochkadagi «uzish» amali.

### 2.3 Mijoz (menejer ulaydi)

1. Avval tashkilot bo'yicha yoqilgan bo'lishi kerak: **Sozlamalar → Telegram (mijozlar)**.
   Sukut bo'yicha **o'chiq** — yoqilmasa, mijozga hech narsa bormaydi.
2. Menejer: **Kontragentlar** → mijoz kartochkasi → Telegram havolasi (ruxsat: `partners.manage`).
   Havola **24 soat** amal qiladi.
3. Mijoz Start bosadi → `✅ <Tashkilot> ga ulandingiz.`

### 2.4 Guruh chat

1. Botni guruhga qo'shing.
2. **Administrator** (profili allaqachon botga ulangan va `settings.modules` ruxsatiga ega odam)
   guruhda `/ulash` yozadi.
3. Bir nechta tashkilotga aloqador bo'lsa, bot tugmalar bilan so'raydi: «Qaysi tashkilot?».
4. Keyin `/sozlash` bilan qaysi turdagi xabarlar kelishini tanlang.

Bir guruh — **bitta tashkilot**. Guruhni uzish: `/uzish`.

---

## 3. Buyruqlar va rollar

Ruxsatlar **har safar buyruq bajarilganda** tekshiriladi. WMS'da rolingiz o'zgarsa,
bot ham darhol shunga moslashadi — qayta ulanish shart emas.

### 3.1 Xodimlar uchun buyruqlar

| Buyruq | Nima qiladi | Kerakli ruxsat |
|---|---|---|
| `/bugun` | Bugungi qisqa holat: yangi va kutayotgan transferlar, faol ishlab chiqarish, kam zaxira, muddati yaqin partiyalar, jami qarzdorlik | `dashboard.view` |
| `/kutilmoqda` | Tasdiq kutayotgan transferlar, har biri ✅/❌ tugmalari bilan (10 tagacha) | `transfers.view` |
| `/qoldiq [nom]` | Mahsulot qoldig'i, masalan `/qoldiq plombir` (5 mahsulot, 15 qatorgacha) | `warehouse.view` |
| `/muddat` | 7 kun ichida muddati tugaydigan partiyalar | `warehouse.view` |
| `/qarz` | Eng katta 15 qarzdor | `finance.view` |
| `/hisobot` | Excel hisobot: zaxira, transferlar, moliya, kontragentlar (faqat ruxsat bor bo'limlar ko'rinadi) | `dashboard.view` + bo'lim ruxsati, tarifda `export.excel` |
| `/keldim` | Ishga kelganingizni belgilaydi (smena so'ralishi mumkin) | `kpi.view`, tarifda `kpi.attendance` |
| `/ketdim` | Ketganingizni belgilaydi va ishlagan vaqtni ko'rsatadi | `kpi.view`, tarifda `kpi.attendance` |
| `/smena` | Bugungi smena rejasi | `kpi.view`, tarifda `kpi.shifts` |
| `/kpi` | Shu haftadagi samaradorlik (reja/fakt) | `kpi.view`, tarifda `kpi.shifts` |
| `/status` | Qaysi tashkilotlarga ulangansiz va nechta xabar turi o'chirilgan | — |
| `/stop` | Barcha tashkilotlardan uziladi | — |
| `/help` | Buyruqlar ro'yxati | — |

Noma'lum buyruq yozsangiz yoki oddiy matn yuborsangiz — bot yordam matnini qaytaradi.

### 3.2 Rol kesimida nima ochiq

| Buyruq / amal | admin | menejer | ishchi | kuzatuvchi |
|---|:---:|:---:|:---:|:---:|
| `/bugun`, `/hisobot` | ✅ | ✅ | ✅ | ✅ |
| `/qoldiq`, `/muddat` | ✅ | ✅ | ✅ | ✅ |
| `/kutilmoqda` (ro'yxatni ko'rish) | ✅ | ✅ | ✅ | ✅ |
| `/qarz` | ✅ | ✅ | ❌ | ✅ |
| `/keldim`, `/ketdim`, `/smena`, `/kpi` | ✅ | ✅ | ✅ | ✅ |
| Transferni ✅ tasdiqlash / ❌ rad etish | ✅ | ✅ | ❌ | ❌ |
| Ishlab chiqarishni ▶️ boshlash | ✅ | ✅ | ✅ | ❌ |
| Guruhni `/ulash`, `/sozlash`, `/uzish` | ✅ | ❌ | ❌ | ❌ |

Ruxsat bo'lmasa bot ochiq aytadi: **«Bu amal uchun ruxsatingiz yo'q.»**
Tarifga kirmasa: **«Bu imkoniyat tarifingizga kirmaydi.»**

> Eslatma: `/kutilmoqda` ro'yxati `transfers.view` bo'lgan hammaga tugmalari bilan keladi,
> lekin tugma bosilganda tasdiqlash huquqi alohida tekshiriladi. Ya'ni ishchi ro'yxatni
> ko'radi, ammo tasdiqlay olmaydi.

### 3.3 Haydovchi

| Buyruq / tugma | Nima qiladi |
|---|---|
| `/marshrut` | Bugungi marshrutni qaytadan yuboradi |
| `✅ <mijoz>` | Shu nuqta yetkazildi deb belgilaydi, mijozga xabar ketadi |
| `❌` | Yetkazilmadi deb belgilaydi, menejerga xabar ketadi (sabab so'ralmaydi) |
| `📄 Yuk xati (PDF)` | Yuk xatini PDF qilib yuboradi |

Haydovchiga WMS roli kerak emas — u tizim foydalanuvchisi emas. Marshrut yaratilganda va
yetkazish boshlanganda xabar o'zi keladi. Boshqa birovning nuqtasini belgilay olmaydi.

### 3.4 Mijoz

| Buyruq | Nima qiladi |
|---|---|
| `/qarzim` | Joriy qarz va oxirgi 5 to'lov |

Mijozga o'zi keladigan xabarlar: buyurtma tasdiqlandi, buyurtma yo'lda, yetkazildi,
to'lov qabul qilindi, qarz eslatmasi.

### 3.5 Guruh chat

| Buyruq | Kim yozadi |
|---|---|
| `/ulash` | Administrator (profili ulangan) |
| `/sozlash` | Administrator — qaysi turdagi xabarlar kelishi (zaxira / transferlar / ishlab chiqarish) |
| `/uzish` | Administrator |

Guruhda **boshqa buyruqlar ishlamaydi** va javob ham qaytmaydi: `/qarz`, `/qoldiq` kabi
so'rovlar ataylab yopilgan, chunki javobni guruhdagi hamma ko'rar edi.

Guruhdagi xabarlardagi ✅/❌/▶️ tugmalarini **har kim emas**, o'sha tashkilotda profili
ulangan va tegishli ruxsatga ega odam bosa oladi. Aks holda: «Avval o'z profilingizni ulang».

---

## 4. Bildirishnomalar

### 4.1 Kimga nima keladi

Xabar faqat **shu ma'lumotni ko'rish huquqi bor** odamlarga boradi:

| Xabar turi | Kimga (ruxsat) |
|---|---|
| Zaxira kam, partiya muddati yaqin / o'tgan | `warehouse.view` |
| Transfer tasdiqlandi / rad etildi, qaytarish qabul qilindi | `transfers.view` |
| Transfer tasdiq kutmoqda | `transfers.confirm` |
| Ishlab chiqarish boshlandi / tugadi | `production.view` |
| Ishlab chiqarish kutmoqda | `production.manage` |
| Yetkazib bo'lmadi | `delivery.manage` |
| Obuna va limit ogohlantirishlari | `settings.modules` (odatda admin) |

Guruhga faqat **umumiy** xabarlar boradi. Shaxsiy xabarlar va obuna/limit ogohlantirishlari
guruhga hech qachon yuborilmaydi.

### 4.2 Keraksiz xabarlarni o'chirish

- **O'zingiz uchun:** WMS → Sozlamalar → Profil → Telegram bo'limida turlarni belgilang
  (zaxira va partiyalar / transferlar / ishlab chiqarish / obuna va tarif).
- **Guruh uchun:** guruhda `/sozlash`.

### 4.3 Vaqt jadvali (Toshkent vaqti)

| Vaqt | Nima |
|---|---|
| **08:00** | Kunlik xulosa (`dashboard.view` bor va xulosa yoqilgan xodimlarga). Kun bo'sh bo'lsa — xabar yuborilmaydi |
| **10:00** | Mijozlarga qarz eslatmasi (tashkilot yoqqan bo'lsa) |
| **22:00 – 07:00** | Jim soatlar: shoshilinch bo'lmagan xabarlar ertalab 07:00 da keladi |

Shoshilinch deb hisoblanadi: zaxira kam, partiya muddati o'tgan, transfer tasdiq kutmoqda,
yetkazib bo'lmadi, obuna to'xtatildi va xatolar — ular jim soatlarda ham darhol keladi.

---

## 5. Bilib qo'yish foydali

- **Til.** Bot Telegram tilingizga qarab javob beradi: ruscha bo'lsa — ruscha, qolgan
  hollarda — **o'zbekcha (lotin)**. Kirill alifbosi hozircha alohida emas.
- **Kechikish.** `/kutilmoqda` javoblari navbat orqali yuboriladi, shuning uchun bir necha
  soniya kechikishi va jim soatlarda ushlanib qolishi mumkin. Qolgan buyruqlar darhol javob beradi.
- **Uzun javoblar** qisqartiriladi: ro'yxatlar oxirida «… va yana N» yoziladi. To'liq ma'lumot — web ilovada.
- **Excel hisobot** 50 MB dan katta bo'lsa yuborilmaydi — web ilovadan yuklab oling.
- **Botni bloklasangiz** yoki guruhdan chiqarsangiz, ulanish avtomatik uziladi. Qayta ulanish —
  profildan yangi havola bilan (blokdan chiqarishning o'zi yetarli emas).
- `/stop` — **barcha** tashkilotlardan uzadi, bittasidan emas.
- Ulanish o'chirilganda sozlamalaringiz saqlanib qoladi: qayta ulansangiz, o'chirilgan
  xabar turlari o'sha holicha qoladi.

---

## 6. Nosozliklar

| Holat | Sabab va yechim |
|---|---|
| «Havola eskirgan yoki allaqachon ishlatilgan» | 10 daqiqa o'tgan yoki havola ishlatilgan — profildan yangisini oling |
| «Hech qaysi tashkilotga ulanmagansiz» | Chat ulanmagan yoki uzilgan — profildan qayta ulang |
| «Bu amal uchun ruxsatingiz yo'q» | Rolingizda bu ruxsat yo'q — administratorga murojaat qiling |
| «Bu imkoniyat tarifingizga kirmaydi» | Modul/tarif yoqilmagan — Sozlamalar → Obuna |
| «Bu tugma eskirgan» | Xabar juda eski yoki hujjat o'zgargan — buyruqni qaytadan yuboring |
| «Allaqachon boshqa odam bajargan» | Transfer/nuqta siz bosguningizcha yopilgan — bu xato emas |
| Mijozga xabar bormayapti | Sozlamalar → Telegram (mijozlar) yoqilmagan bo'lishi mumkin |
| Guruhda buyruq javobsiz | Guruhda faqat `/ulash`, `/sozlash`, `/uzish` ishlaydi |

---

## 7. Administrator uchun sozlash

Server `.env` (prod: `/opt/agentics/wms/docker/.env`):

| Kalit | Ma'nosi |
|---|---|
| `TELEGRAM_BOT_TOKEN` | @BotFather tokeni. **Bo'sh bo'lsa bot butunlay o'chiq** (xato emas). Dev va prod uchun **har xil bot** bo'lishi shart — bitta token ikki joyda polling qilsa Telegram 409 beradi |
| `TELEGRAM_OPS_CHAT_ID` | Platforma egasining chati: yangi tenant, obuna ogohlantirishi, xizmat ishga tushgani. Mijoz ma'lumotlari bu yerga yuborilmaydi |
| `PUBLIC_WEB_URL` | Xabarlardagi «Ochish» havolasi shu manzilga qarab quriladi |

Token o'zgargach `wms-api` ni qayta ko'tarish kerak:

```bash
cd /opt/agentics/wms/docker && docker compose -f docker-compose.prod.yml up -d --no-build --no-deps wms-api
docker logs wms-api --since 2m | grep -i telegram     # «Telegram bot @… ishga tushdi (polling)»
```

Bot nomi sozlanmaydi — ishga tushganda Telegram'dan o'zi olinadi. Webhook yo'q, faqat polling.
