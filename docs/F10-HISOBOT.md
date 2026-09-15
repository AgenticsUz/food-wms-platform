# F10 — bosqich hisobotlari

> Har bosqich (P → A → K) yakunida shu faylga qisqa hisobot yoziladi: nima
> qilindi, qabul mezoni natijasi, ochiq qolganlar. Reja — `docs/AI/WMS-AI-REJA.md`,
> ish varag'i — `docs/AI/P-CHEKLIST.md`.

---

## P0 — ochiq bandlarni yopish (2026-09-14)

### §3 SQL tekshiruvi — `01a0964f-…` hujjatining sababi ANIQLANDI

Prod (`45.138.158.200`, `agentics_wms`) da faqat o'qish so'rovlari:

| So'rov | Natija |
|---|---|
| Hujjat va qatorlari | `type=2` (Outgoing), `status=1` (Pending), manba ombor `01a0964c-…` (**Sklad1**), mahsulot `01a09648-…` (**Snikers**), miqdor **34** |
| O'sha mahsulot bo'yicha qoldiq qatorlari | **0 qator** |
| Butun bazadagi qoldiq qatorlari | 14 ta, ulardan **o'chirilgan partiyali — 0 ta** |
| Omborlar kesimi | Xom ashyo ombori 8, Tayyor mahsulot ombori 6, **Sklad1 — 0** |

**Xulosa.** Ikkala gumon ham tasdiqlanmadi: qoldiq boshqa omborda ham turmagan,
yumshoq o'chirilgan partiya bazada umuman yo'q. «Snikers» mahsulotiga hech qachon
kirim qilinmagan — ya'ni tasdiqdagi «omborda yetarli qoldiq yo'q» xatosi
MAZMUNAN TO'G'RI edi. Haqiqiy nuqson faqat foydalanuvchi tajribasida: xato
hujjat yaratilganidan keyin, tasdiq bosqichida chiqqan va qancha yetmaganini
aytmagan — ikkalasi ham T2 (`096f2be`) da tuzatilgan
(`EnsureStockAvailableAsync` + «kerak N, mavjud M» xabari).

### O'chirilgan partiya masalasi (XATOLAR §3, 4-ish) — qaror va tuzatish

Prod'da bunday qator yo'q, lekin nuqson LATENT holda turardi: `StockAllocator`
partiyaga `EXISTS` qilardi (yumshoq o'chirish filtri bilan), qoldiq ekrani
(`WarehouseService.GetStockAsync`) esa partiyaga umuman tegmaydi — «ekranda bor,
chiqimga chiqmaydi» holati.

**Qaror:** haqiqat — qoldiq qatorining o'zi; partiya faqat FEFO tartibi uchun
metama'lumot va uni o'chirish tovarni omborda yo'q qilmaydi. `AvailableRows`
endi yumshoq-o'chirish filtrini o'chirib, qoldiq qatorining O'Z `is_deleted` ini
tekshiradi. Darvoza testlari — `tests/WMS.Tests/Catalog/StockAllocatorTests.cs`.

---

## P1 — backend test poydevori (2026-09-14)

`tests/WMS.Tests` — xUnit v3 + Testcontainers PostgreSQL 17, InMemory EMAS:
tekshiriladigan da'volar bazaning o'ziga tayanadi (RLS siyosati, `xmin`,
filtrli noyob indeks, `numeric(18,3)`).

- Fixture konteynerni ko'taradi, init skriptining test nusxasini yuritadi
  (kengaytmalar + `app_user` **NOBYPASSRLS** / `app_migrator` rollari), haqiqiy
  migratsiyani (`WmsDatabaseMigrator`, RLS generatori bilan) qo'llaydi va TO'LIQ
  DI grafini quradi (`AddWmsInfrastructure` + `AddWmsModules`).
- Har test O'Z tenantini oladi — testlar orasida `TRUNCATE` yo'q, ajratishni RLS
  qiladi (bu ham o'lchanadi).
- `scripts/run-tests.sh` — platforma naqshi, ikki darvoza: DLL yo'q → xato,
  `Total: 0` → xato.

**Qamrov (44 test, hammasi yashil):**

| To'plam | Nima o'lchanadi |
|---|---|
| `Infrastructure/RlsIsolationTests` (2) | Boshqa tenant qatorlari ko'rinmaydi; kontekstsiz qamrov 0 qator (global filtr O'CHIRIB tekshiriladi — himoya RLS'da) |
| `Infrastructure/RlsPolicyTests` (6) | `app_user` NOBYPASSRLS va superuser emas; 5 jadvalda `tenant_isolation` + `FORCE` |
| `Trade/TransferServiceTests` (6) | FEFO tartibi, «kerak N, mavjud M» YARATISHDA, kirimda partiya, parallel 409, rad etish, ichki ko'chirish |
| `Trade/TransferCommissionTests` (3) | Komissiya summasi/yaxlitlash, ikkinchi tasdiq, noyob indeks |
| `Trade/TransferPermissionGateTests` (6) | Rol to'plamlari va `[RequirePermission]` atributlari (refleksiya) |
| `Catalog/StockAllocatorTests` (6) | «Qancha bor» = «yechib ber»; o'chirilgan partiya/qator; muddatsiz partiya oxirida |
| `Catalog/SearchServiceTests` (6) | Uch alifboda bir mahsulot; 3 nomzod; tenant ajratilishi |
| `Catalog/ProductSearchPathTests` (3) | UI yuradigan yo'l: `GetAllAsync(search:)`, `name_search` yaratish/tahrirda yoziladi |

**Darvozalar HAQIQATAN qizil beradimi** (qabul mezoni). Vaqtinchalik `git worktree`
da eski kodga qarshi yurgizildi (asl daraxtga tegilmadi):

| Darvoza | Qaysi holatda qizil |
|---|---|
| «O'chirilgan partiyali qator FEFO'da ko'rinadi» | FEFO tuzatishidan oldingi commit (`EXISTS` sharti) |
| «Ikki yuza bir xil javob beradi» | O'sha commit |
| «Qoldiq yetmasa YARATISHDA xato» | `EnsureStockAvailableAsync` olib tashlanganda |

CI: `.github/workflows/ci.yml` ga `integration` job'i qo'shildi —
`bash scripts/run-tests.sh Release` (`dotnet test` EMAS).

---

## P2.1 — nom qidiruvi (2026-09-14)

«Сникерс» deb yozgan odam «Snikers» ni topa olmasdi: qidiruv mijozda `includes`
bilan ishlardi. `unaccent` bu ishni qilmaydi (u faqat diakritika), shuning uchun:

- `SearchNormalizer` (C#): kichik harf → kirill lotinga (o'zbek yozuvi) →
  apostrof tashlanadi → belgilar bo'sh joyga siqiladi. «Сникерс» → `snikers`.
- `name_search` ustuni + GIN trigram indeks; ustunni servislar, import va demo
  seed to'ldiradi; migratsiya eski qatorlarni backfill qiladi.
- **Backfill va RLS tuzog'i:** migratsiya `app_migrator` bilan yuradi va tenant
  jadvallarida `FORCE` RLS tufayli `UPDATE` JIM 0 qatorga tegardi (XATOLAR §9.3).
  Yechim — har jadval uchun `NO FORCE` → `UPDATE` → `FORCE`. Lokal stendda
  tekshirildi: bo'sh `name_search` qolgan qator **0 ta**.
- `ISearchService` nomzodlarni BAL bilan qaytaradi va «eng yaxshisi» ni
  tanlamaydi — A1 da AI bir nechta yaqin nomzodda qayta so'rashi uchun.
- UI: `GET /api/products?search=`, `GET /api/counterparties?search=`, 300 ms
  debounce; mahsulot turi filtri mijozda qoldi (backend uni bilmaydi).

**Ochiq (A1 dan oldin ko'rib chiqiladi):** GIN indeks so'rovda ishlatilmayapti
(seans chegarasi «snikers»↔«snickers» ni kesardi); transliteratsiya o'zbekcha
(ruscha `zh` past bal beradi); o'xshashlik chegarasi 0.4 — real katalogda sozlash
kerak bo'lishi mumkin.

---

## Ochiq qolgan ishlar (2026-09-14 holatiga)

| Ish | Kimda |
|---|---|
| ~~Prod deploy (P0 №1)~~ | ✅ 2026-09-15 09:15 |
| Bot tokenini BotFather'da `/revoke` va prod `wms.env` ga yangisi (P0 №4) | foydalanuvchi |
| Kabinet ekranlarini brauzerda ko'rish (3-BLOK №1) | foydalanuvchi |
| 2-BLOK: P2.2–P2.7 (P2.7 — Variant A qaroriga ko'ra) | keyingi sessiya |
| Qarz #17: WMS nginx CSP (3-BLOK №2) | keyingi sessiya |

---

## Prod deploy — 2026-09-15 09:15

Zaxira `wms-20260915-0911.dump` (227K), eski image'lar `pre-20260915-0914` teglari
bilan saqlandi. `F10_NameSearch` qo'llandi; `wms-api`/`wms-web`/`wms-postgres`
healthy, `https://wms.agentics.uz/login` → 200, tokensiz `/api/products` → 401.

Prod tekshiruvi:
- `name_search` backfill — 11 mahsulotning hammasi to'ldi, bo'sh **0 ta**;
- qidiruv ishlaydi: `word_similarity('snikers', name_search)` → «Snikers» = 1.000;
- ⚠️ eng muhimi — backfill'dan keyin RLS TIKLANGAN: `product`, `counterparty`,
  `warehouse`, `transfer` da `relrowsecurity = t`, `relforcerowsecurity = t` va
  `tenant_isolation` siyosati joyida.

### Deploy paytida ko'rilgan ESKI nuqson (bu bosqichga aloqasi yo'q)

Ko'tarilishdan keyin bir marta: `Fon vazifasi tenant demo uchun yiqildi` →
`23505: duplicate key value violates unique constraint "ix_telegram_outbox_dedup_key"`.
Ya'ni fon vazifasi navbatga allaqachon qo'yilgan xabarni qayta qo'yishga urinadi
va 23505 ni USHLAMAYDI — natijada o'sha aylanish to'liq uziladi (boshqa tenantlar
va keyingi ishlar bajarilmay qoladi). Naqsh `DebtLedger.GetOrCreateAsync` da bor
(23505 yutiladi) — shu yerda ham kerak. Deploy'dan oldin ham shunday bo'lgan
(kod bu bosqichda o'zgarmagan). Tuzatish — alohida ish sifatida navbatga.


---

## P2.2–P2.9 — domen bo'shliqlari (2026-09-15)

> Foydalanuvchi talabi: «AI'siz ham, AI bilan ham muammo bo'lmasligi kerak».
> Shuning uchun har band AI uchun emas, TIZIM uchun tuzatildi; AI qatlami ularni
> keyin shunchaki ishlatadi.

### Nima qilindi

| Band | Mazmuni |
|---|---|
| P2.2 | `IPricingService` — oxirgi TASDIQLANGAN hujjatdagi narx: avval shu kontragent bilan, bo'lmasa umumiy; manbasi (hujjat, sana, kontragent) bilan qaytadi |
| P2.3 | `Transfer.DocumentDate` — hujjat sanasi; filtr, hisobot, eksport, PDF va Telegram SHU ustunga o'tdi |
| P2.4 | `Transfer.Number` / `ProductionOrder.Number` — tenant ichida ketma-ket, `tenant_counter` bilan |
| P2.5 | `Batch.UnitCost`, `TransferItem.UnitCost`, `StageExecution.MaterialCost` + `GetProductProfit` |
| P2.6 | Standart ombor: tenant sozlamasi + xodim override'i + bitta ombor holati |
| P2.7 | `Product.PackSize` / `PackUnit` (Variant A) — konversiya FORMADA, baza doim asosiy birlikda |
| P2.8 | `PaymentHistory.DocumentDate`, `.Direction`, `.Source` |
| P2.9 | `ReversePaymentAsync` — storno (o'chirish EMAS) |

### Koddan topilgan haqiqiy nuqsonlar (hammasi tuzatildi)

1. **Bir davr — uch xil natija.** Hisobot `ConfirmedAt`, ro'yxat va Excel `CreatedAt`,
   ishlab chiqarish yana `CreatedAt` bo'yicha ishlardi. Endi hammasi `DocumentDate`
   (izohlangan istisnolar bundan mustasno: «qachon tasdiqlandi» va «bugun nima
   KIRITILDI» — boshqa savol, boshqa ustun).
2. **To'lov yo'nalishi taxmin qilinardi** (`CreatePaymentDto.Direction` bo'sh bo'lsa
   qarz belgisidan). Nol balansda bu summani teskari tomonga yozib, xatoni IKKI
   barobar qilardi. Endi yo'nalish SAQLANADI va nol balansda servis so'raydi.
3. **To'lovni tuzatish yo'li umuman yo'q edi** — xato summa qarz balansida abadiy
   qolardi. Endi storno: teskari yozuv + sabab, ikki marta qaytarib bo'lmaydi,
   mijozga xabar ketadi (unga to'lov haqida allaqachon yozilgan bo'lishi mumkin).
4. **Telegram kunlik xulosa** bir kunda qayta ko'tarilganda `23505` (dedup kaliti)
   bilan yiqilib, `ForEachTenantAsync` ni O'SHA TENANTDA uzardi — qolgan ishlar
   bajarilmasdi. Prod deploy'ida ko'rindi. Endi kalitlar oldindan o'qiladi, 23505
   esa zaxira to'r (`PostgresErrors`); marshrut va mijoz xabarlarida ham shunday.
5. **Migratsiya backfill'i jimgina 0 qatorga tegdi** — `FORCE ROW LEVEL SECURITY`
   O'QISHGA ham qo'llanadi, `batch.unit_cost` ni to'ldiruvchi `UPDATE` esa
   `transfer_item` dan o'qirdi va u `NO FORCE` ro'yxatida yo'q edi. **Bo'sh test
   bazasida ko'rinmasdi** — lokal stendda, ma'lumotli bazada o'lchab topildi.
   Saboq `docs/DEPLOY.md` §5.1 ga yozildi.

### Darvozalar

- `RlsPolicyTests` endi jadval ro'yxatini QO'LDA sanamaydi: EF modelidan
  `ITenantEntity` bo'yicha oladi va har birida RLS + `FORCE` + `tenant_isolation`
  borligini tekshiradi. ⚠️ Mezon «`tenant_id` ustuni bor» EMAS — Telegram
  jadvallarida ham shu ustun bor, lekin ular ataylab platforma jadvallari.
- To'lov testlari: nol balansda yo'nalish so'raladi; kelajak sana rad etiladi;
  orqaga sana faqat ruxsat bilan; storno qarzni qaytaradi va takrorlanmaydi.
- Hujjat testlari: 20 parallel yaratish → 20 noyob raqam; kecha sanali hujjat
  kechagi hisobotda; Excel va analitika bir xil to'plamni qaytaradi.

### Holat

- Backend: **84 test yashil** (44 → 84), build 0 xato / 0 ogohlantirish.
- Frontend: `lint`, `test`, `build` kesh'siz yashil.
- Migratsiyalar (`F10_DocumentsAndCosts`, `F10_ProductionMaterialCost`) lokal stendda
  HAQIQIY ma'lumotda sinaldi: 18 hujjatga raqam berildi, hisoblagich 18/6 da,
  13 partiyadan 9 tasi tannarx oldi (qolgani ishlab chiqarish partiyalari — kirim
  narxi yo'q), RLS `FORCE` hamma jadvalda joyida.

### Ochiq qolganlar (AI bosqichidan oldin ko'rib chiqiladi)

- Qaytarish (`Return`) mahsulot foydasidan AYIRILMAYDI — reja bu haqda jim; kerak
  bo'lsa alohida ish.
- Mahalliy kun ↔ UTC kun chegarasi: eksport xom taqqoslaydi, analitika kunga
  yaxlitlaydi; Toshkent (+5) kun chegarasini yuborganda bir kunga farq qilishi
  mumkin — ikkala joyda BIRGA hal qilinishi kerak.
- `moduleGuard` bitta modul kodini qabul qiladi, backend esa «kamida bittasi»
  qoidasida — standart ombor sahifasida modul guard'i qo'yilmadi.

---

## P2 prod deploy — 2026-09-15 12:17

Zaxira `wms-p2-20260915-1216.dump` (231K), eski image'lar `pre-20260915-1217` teglari
bilan. `F10_DocumentsAndCosts` va `F10_ProductionMaterialCost` qo'llandi; uchala
konteyner healthy, `https://wms.agentics.uz/login` → 200.

Prod'dagi natija (baza):

| Tekshiruv | Natija |
|---|---|
| Hujjatlar | 30 ta, **raqamsizi 0** |
| Hisoblagichlar | ikki tenantda `transfer` = 24 va 6, `production_order` = 6 — hujjatlar soni bilan mos |
| Partiyalar | 17 ta, **13 tasi tannarxli** (qolgani ishlab chiqarish partiyalari — kirim narxi yo'q) |
| To'lovlar | 15 ta, hammasi `In` (backfill qoidasi bo'yicha) |
| RLS | `transfer`, `transfer_item`, `payment_history`, `batch`, `tenant_counter` — hammasida `rls` va `FORCE` yoqiq |

Prod API (haqiqiy token bilan) tekshirildi:

- `GET /api/transfers` → `number: 24`, `documentDate: 2026-09-12`, `source: 1`;
- `GET /api/pricing/last-price` → 23 000, manbasi `#24`, `isSameCounterparty: false`
  (boshqa kontragent narxi — UI uni «umumiy narx» deb kulrang ko'rsatadi);
- `GET /api/settings/warehouses` → sozlanmagan (hamma `null`, tenantda 3 ombor);
- `GET /api/finance/payments` → `direction`, `documentDate`, `source`, `isReversed`.

⚠️ Eski chiqim qatorlarida `unitCost` — `null`: tarixda qaysi partiya sotilgani
yozilmagan va uni qayta tiklab bo'lmaydi. Yangi hujjatlarda to'ldiriladi; foyda
hisoboti bunday qatorlarni `unknownCostQuantity` sifatida alohida ko'rsatadi.
