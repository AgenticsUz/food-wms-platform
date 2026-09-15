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

---

## A0 — AI poydevori (2026-09-15)

Reja: `docs/AI/WMS-AI-REJA.md` §A0. Ish varag'i: `docs/AI/P-CHEKLIST.md`.
Bu bosqichda hech narsa «gaplashmaydi» — faqat poydevor: provayder seam'i, tool
registri, jadvallar, sarf hisobi va o'chirgichlar.

### Nima qilindi

**Provayder seam'i.** `ILlmClient` (Application) — WMS kodining model bilan
gaplashadigan yagona joyi; `AnthropicLlmClient` (Infrastructure) Anthropic SDK
turlarini biladigan yagona sinf (`Anthropic 12.47.0`, versiya ANIQ qadalgan).
System prompt ikki blokka bo'lingan (barqaror + o'zgaruvchan): kesh prefiksi
`tools → system → messages` tartibida yig'iladi, ya'ni sana yoki tenant nomi
barqaror blokka tushsa prefiks har so'rovda o'zgarib, kesh hech qachon urmasdi.

**Tool registri — xavfsizlik chegarasi.** `IAiTool { Code, Description, Schema,
PermissionCode, FeatureCode?, ExecuteAsync }`. Registr foydalanuvchi ruxsatlari VA
tenant feature'lariga qarab filtrlanadi; ruxsatsiz tool modelga umuman
ko'rsatilmaydi. Bajarishdan oldin tekshiruv QAYTA qilinadi (`Require`): model
ro'yxatda ko'rmagan nomni o'zi to'qib chaqirishi mumkin. Takror tool kodi
startupda yiqitadi — jimgina «oxirgisi yutadi» bo'lsa, ko'rinadigan nom ortida
kutilmagan amal turishi mumkin edi.

**Jadvallar** (`F10_AiFoundation`): `ai_conversation`, `ai_message`, `ai_usage`
(uchalasi RLS ostida) va `ai_daily_cost` (tenant ustuni yo'q, RLS yo'q). Oxirgisi
ATAYLAB alohida: kunlik dollar shifti hamma tenant yig'indisiga qaraydi va RLS
ostidagi jadvaldan bunday yig'indi olib bo'lmaydi. Audit havolasi:
`transfer.ai_conversation_id` va `payment_history.ai_conversation_id` (FK
`RESTRICT` — havola qilingan suhbat tarix tozalashda o'chmaydi).

**Metering.** `ai_usage` `INSERT … ON CONFLICT DO UPDATE … + EXCLUDED` bilan
yoziladi (`DocumentNumbers` naqshi): bir tenantdan parallel kelgan ikki so'rov bir
xil kunga yozadi va o'qib-o'zgartirib-yozish oxirgisining tokenini yo'q qilardi.

**O'chirgichlar — uch qatlam:** (1) `Ai__ApiKey` bo'sh — modul o'chiq, qolgan tizim
ishlayveradi; (2) `ai.chat` feature'i — Console'dagi to'liq o'chirgich, hech bir
planga kirmaydi va sukuti o'chiq; (3) plan kvotasi (`MaxAiRequestsPerMonth`) va
platformaning kunlik dollar shifti (`Ai:DailyUsdCap`).

### Qabul mezoni natijasi

| Mezon | Natija |
|---|---|
| `dotnet build` | 0 xato / 0 ogohlantirish |
| `bash scripts/run-tests.sh` | **102 test yashil** (84 → 102, 18 yangi) |
| `Ai__ApiKey` bo'sh → modul o'chiq | ✅ test bilan (`Kalit_yoq_bolsa_ai_ochiq`) |
| `ai.chat` katalogda, demo tenantga yoqilgan | ✅ `BaseCatalogSeeder` + `DemoSeeder` |
| Migratsiya | Testcontainers'da haqiqiy Postgres'ga qo'llandi; RLS darvozasi
  (`RlsPolicyTests`, model bo'yicha) yangi jadvallarni o'z-o'zidan qamradi |

### Yo'l-yo'lakay tuzatilgan nuqsonlar

A0 kodining bir qismi oldingi sessiyadan commit qilinmagan holda qolgan edi; unda
ikkita nuqson topildi:

- `AiToolSchema.For<T>()` — `JsonSchemaExporter` sukut bo'yicha havola turini
  «null bo'lishi ham mumkin» deb belgilaydi va ildiz sxemasi `["object","null"]`
  bo'lib chiqardi. Natijada sxema eksporti O'Z tekshiruvida yiqilardi.
  `TreatNullObliviousAsNonNullable = true` qo'shildi.
- `AnthropicLlmClient.ParseEffort` `xhigh` ni bilmasdi, `AiOptions` esa uni
  hujjatlashtirgan edi: `Ai__Effort=xhigh` birinchi JONLI chaqiriqda yiqilardi
  (SDK'da `Effort.Xhigh` bor).

### Ochiq qolganlar

- Suhbat tarixini tozalash fon vazifasi — siyosat qiymati (`HistoryRetentionDays`)
  bor, vazifaning o'zi A1 da.
- `ai_usage` ni Console'da ko'rsatish (hozir faqat bazada).
- Prod'ga deploy QILINMADI: `Ai__ApiKey` bo'sh bo'lsa modul o'chiq, ya'ni A0 ni
  alohida deploy qilishning ma'nosi yo'q — A1 bilan birga chiqadi.

---

## A1 — O'quvchi tool'lar, gateway va Telegram (2026-09-15)

AI endi savolga javob beradi. Hech narsa yozmaydi: A1 ning hamma tool'i o'quvchi.

### Nima qilindi

**10 ta tool.** `find_product`, `find_counterparty`, `stock_query`, `expiry_query`,
`debt_query`, `payment_history`, `finance_summary`, `pending_transfers`,
`last_price`, `today_summary`. Har biri mavjud servisni chaqiradi (§0.4) va natijani
ikki shaklda qaytaradi: modelga qisqa matn, yuzaga strukturali DTO. Ro'yxat 50
qatordan uzun bo'lsa matn buni OSHKORA aytadi — model «hammasi shu» deb javob
bermasin.

**Noaniqlik — savol.** `AiNameResolver` qoidasi bitta joyda: bitta nomzod
ishlatiladi, bir nechtasi — nomzodlar sanaladi va tanlov foydalanuvchiga
qaytariladi. Bal bo'yicha «yetarlicha ustun» degan yumshoq qoida ATAYLAB yo'q:
`word_similarity` bali nom uzunligiga qarab suzadi va chegara qachondir noto'g'ri
tomonga og'ardi.

**Gateway.** Metering darvozasi → ruxsat va feature bo'yicha filtrlangan tool
ro'yxati → maksimum 6 aylanish → javob. Tool istisnosi suhbatni uzmaydi (xato
natija bo'lib modelga qaytadi); istisno MATNI modelga uzatilmaydi — ichki tafsilot
javob orqali foydalanuvchiga chiqib ketmasin.

**Suhbat oynasi.** Bir soat jimlik, oxirgi 10 xabar. Tarix kesilganda boshidan
birinchi foydalanuvchi savoligacha tashlanadi: `tool_use` juftligi buzilsa
provayder butun so'rovni 400 bilan rad etardi.

**Telegram.** Buyruq bo'lmagan matn gateway'ga o'tadi. Buyruqlar (`/qoldiq`,
`/qarz` …) o'z ishlovchisida QOLADI — ular aniq, tez va bepul. Ulanmagan chat eski
yo'riqnomani ko'radi: AI qo'shilgani uchun hech kimning boti «ishlamay qolgandek»
ko'rinmasin. «Yozmoqda…» ko'rsatkichi va 4096 belgida bo'linish qo'shildi.

### Sinov to'plami — ikki rejim

| Rejim | Nimani o'lchaydi | Qachon |
|---|---|---|
| Fixture | Kutilgan tool o'z argumentlari bilan TO'G'RI javob beradimi | Har commit'da, pulsiz |
| Jonli (`AI_EVAL_LIVE=1`) | Model to'g'ri tool tanladimi, noaniqda so'radimi | Qo'lda, kalit bilan |

Ajratilgani ataylab: ball past bo'lganda «model yanglishdimi yoki WMS noto'g'ri son
berdimi?» degan savolga javob kerak. Jonli rejim `ai_usage` ga YOZMAYDI — sinov
mijoz kvotasini yemasin.

50 savol: qoldiq 10, muddat 5, qarz 8, bugungi 5, kutilayotgan 5, narx 5,
noaniq nom 7, ruxsatsiz 5. Ma'lumot to'plamida «Plombir» uchta mahsulotga,
«Korzinka» ikkita kontragentga to'g'ri keladi.

### Qabul mezoni natijasi

| Mezon | Natija |
|---|---|
| `dotnet build` | 0 xato / 0 ogohlantirish |
| `bash scripts/run-tests.sh` | **119 test** (118 yashil + 1 jonli, o'tkazib yuborilgan) |
| Ruxsatsiz tool modelga berilmaydi | ✅ gateway testi (`debt_query` ombor foydalanuvchisiga ko'rinmaydi) |
| Zo'rlab chaqirilgan tool rad etiladi | ✅ `ai_tool_forbidden`, suhbat uzilmaydi |
| Fixture to'plami | ✅ 50/50 — har savolning kutilgan tool'i to'g'ri javob berdi |
| Jonli to'plam ≥ 45/50 | ⏳ **kalit kerak** |
| Botda 20 real savol | ⏳ **foydalanuvchi tekshiruvi** |

### Yo'l-yo'lakay tuzatilgan nuqson

`LlmRequest.Messages` ga ro'yxatning O'ZI (havola) berilardi va u sikl davomida
o'sib borardi — ya'ni provayderga ketgan so'rov keyin jimgina «o'zgarib» qolardi.
Prod'da SDK so'rovni darhol serializatsiya qilgani uchun bu ko'rinmasdi, lekin
jurnal, qayta urinish va test o'sha so'rovni BOSHQACHA ko'rardi. Endi nusxa
uzatiladi.

### Ochiq qolganlar

- Jonli eval va botdagi qo'l tekshiruvi — ikkalasi ham `Ai__ApiKey` ga bog'liq.
- Suhbat tarixini tozalash fon vazifasi (`HistoryRetentionDays` siyosat qiymati bor).
- Ko'p tenantli chatda tanlovdan keyin savol qayta so'raladi (`callback_data` ≤ 64 bayt).

---

## A2 — Web panel va kabinet (2026-09-15)

AI endi brauzerda: qobiqdagi global tortma, javob oqim bo'lib keladi.

### Nima qilindi

**Gateway oqimga aylandi.** `StreamAsync` hodisalar beradi (`tool_result` → `text`
→ `done`), `AskAsync` esa o'sha oqimni yig'adi — ya'ni sikl BITTA joyda qoldi.
Ikki nusxa bo'lsa, «tool'siz raqamli javob bermaslik» kabi qoidalar bir yuzada
kuchga kirib, ikkinchisida jimgina yo'qolardi.

⚠️ Hodisalar QADAM darajasida, token-token emas: harflar paydo bo'lishi provayder
oqimini talab qiladi va `ILlmClient` uni hozir bilmaydi. Kalitsiz yozilgan
streaming kodini sinab ko'rib bo'lmasdi, shuning uchun u A3 ga qoldirildi.

**Yuzalar.** `POST /api/ai/chat` (`text/event-stream`),
`GET /api/ai/conversations`, `GET /api/ai/conversations/{id}`.
`EventSource` ishlatilmadi — u `Authorization` sarlavhasini yubora olmaydi;
mijoz `fetch` + `ReadableStream` bilan o'qiydi. `X-Accel-Buffering: no` —
nginx oqimni buferlab, hamma hodisani oxirida birdan bermasin.

**Panel.** Qobiqdagi o'ng tortma, holat `AiStore` da (`providedIn: 'root'`):
sahifa almashganda suhbat saqlanadi. Tool natijalari komponent bilan —
`stock_query` jadval, `debt_query` kontragent havolasi bilan jadval,
`pending_transfers` hujjat havolalari. Shakl `unknown` dan tekshirib o'qiladi:
backend DTO'si o'zgarsa panel jim qoladi (matn baribir ko'rinadi).

**Kabinet.** `POST /api/portal/ai/chat` — o'sha gateway, boshqa tool to'plami
(`my_debt`, `my_transfers`). Ruxsat kodi `portal.self` RBAC katalogida YO'Q, ya'ni
zavod xodimi kabinet tool'larini hech qachon ko'rmaydi; kabinet foydalanuvchisi esa
faqat shularni ko'radi. Himoya ikki qatlamli: tool'lar `IPortalService` ni chaqiradi
va u tokendagi `sub` ni kartaga bog'lay olmasa 403 beradi (fail-closed).

### Qabul mezoni natijasi

| Mezon | Natija |
|---|---|
| `dotnet build` | 0 xato / 0 ogohlantirish |
| `run-tests.sh` | **124 test** (123 yashil + 1 jonli, o'tkazib yuborilgan) |
| `wms-web` lint / test / build | ✅ uchalasi yashil (frontend testlari 21 → 25) |
| Kabinet va zavod tool'lari ajratilgan | ✅ test bilan |
| Suhbat faqat egasiga | ✅ begonaga 404 |
| 10 savol web'da, brauzer tekshiruvi | ⏳ **kalit + foydalanuvchi** |

### Ochiq qolganlar

- Token-token oqim — kalit bo'lgach `ILlmClient` ga qo'shiladi va jonli sinaladi.
- Suhbat tarixi yuzasi: API bor, panelda ro'yxat hali chizilmagan.
- Kabinetda tarix yo'q: kabinet foydalanuvchisida `user_profile.id` bo'lmaydi, ya'ni
  suhbat egasini hozirgi shaklda yozib bo'lmaydi (A3 da ko'riladi).

---

## A3 — Yozuvchi amallar (2026-09-15)

AI endi hujjat va to'lov QORALAMASINI tayyorlaydi. Yozuvni hamon odam yaratadi.

### Nima qilindi

**`draft_transfer`.** Kirim/chiqim qoralamasi: nomlar yechiladi (noaniqda savol),
narx berilmasa chiqimda oxirgi sotuv narxi va kirimda tannarx qo'yiladi, qadoq
asosiy birlikka o'giriladi (50 quti → 600 dona), chiqimda mavjud qoldiq
tekshiriladi. Natijada narx QAYERDAN olingani ham aytiladi — model «12 000» ni
o'zi topib qo'ygandek gapirmasin.

**`draft_payment`.** Balans oldi/keyin bilan: «2 000 000 yozilsinmi?» degan savolga
odam balansni ko'rmasdan javob bera olmaydi. Yo'nalish MAJBURIY va hech qachon
taxmin qilinmaydi.

**Tasdiqlash yuzasi.** `POST /api/ai/drafts/transfer` va `.../payment` — alohida
controller, tool emas: model bu manzillarni ko'rmaydi ham, chaqira ham olmaydi.
`Source = ai` va suhbat id'sini SERVER qo'yadi, mijoz emas.

**Web.** Qoralama alohida karta bo'lib chiziladi (tool natijasidan ko'ra ko'zga
tashlanadi — bu yerda foydalanuvchi QAROR qabul qiladi), tugma bir marta bosiladi.

### Qabul mezoni natijasi

| Mezon | Natija |
|---|---|
| `dotnet build` | 0 xato / 0 ogohlantirish |
| `run-tests.sh` | **132 test** (131 yashil + 1 jonli) |
| `wms-web` lint / test / build | ✅ uchalasi yashil |
| AI hujjat yaratmaydi | ✅ qoralamadan keyin bazada 0 hujjat |
| Tasdiqlovchi tool yo'q | ✅ statik darvoza (hamma ruxsat bilan ham topilmaydi) |
| Yo'nalish taxmin qilinmaydi | ✅ yo'nalishsiz qoralama tayyorlanmaydi |
| Fixture to'plami | ✅ **77/77** (50 → 77) |
| Jonli to'plam | ⏳ kalit kerak |

### Qilinmagan

**Telegram tugmali tasdiq.** Qoralamani tugmaga sig'dirib bo'lmaydi
(`callback_data` ≤ 64 bayt) — qoralamani saqlaydigan joy kerak. Yarim qilib qo'yish
«bosdim, hech narsa bo'lmadi» holatiga olib borardi, shuning uchun botda qoralama
hozir MATN bo'lib ko'rinadi va foydalanuvchi uni ilovada yaratadi. Alohida ish
sifatida ochiq qoldirildi.

**`draft_transaction`** (kontragentsiz kirim/chiqim) — reja uni ixtiyoriy deb
belgilagan.

---

## Jonli sinov — birinchi yurish (2026-09-15)

Kalit qo'yilgach sinov to'plami HAQIQIY model bilan yurgizildi. To'liq 77 savol
EMAS: byudjet foydalanuvchining prod/bot tekshiruvi uchun saqlandi, shuning uchun
14 ta savol — oqimni ochadigan va eng xavfli joylarni qamraydigan tanlov.

### Natija

| To'plam | Savollar | Natija |
|---|---|---|
| Qoldiq (birinchi 8) | `stock-01…08` | **8/8** |
| Nuqtali (xavfli joylar) | `denied-01`, `vague-01`, `draft-01`, `pay-05` | **4/4** (ikki tuzatishdan keyin) |

Tool zanjirlari kutilgandek: «Snikers qancha qoldi?» → `find_product` →
`stock_query`; «Asosiy skladdan Makroga 10 dona Snikers» → `find_product` →
`find_counterparty` → `draft_transfer`.

### Topilgan nuqson — PROD'NI YIQITADIGAN

`AiToolSchema.SerializerOptions` da `TypeInfoResolver` OSHKORA berilmagan edi.
`JsonSchemaExporter` sozlamalarni faqat o'qishga belgilaydi va resolver hali
yo'q bo'lsa «read-only» xatosi bilan yiqiladi.

⚠️ Nuqson TARTIBGA bog'liq edi: 133 testli to'plam uni KO'RMADI, chunki biror test
avval deserializatsiya qilib sozlamalarni to'ldirib qo'yardi. Gateway esa sxemani
BIRINCHI so'raydi — ya'ni prod'da AI birinchi savoldayoq 500 berardi va buni faqat
mijoz ko'rgan bo'lardi.

Tuzatildi; tartibga bog'liq bo'lmagan darvoza qo'shildi: `HAR_toolning_sxemasi_quriladi`
(har ro'yxatdan o'tgan tool'ning sxemasi quriladi va ildizi `object` ekani tekshiriladi).

### Sinov to'plamining ikki kamchiligi (model emas, to'plam)

Ikkalasi ham fixture rejimida KO'RINMAYDI, chunki u argumentlarni tayyor oladi —
jonli rejim esa savol MATNIDAN boshlaydi:

- **`draft-01`** savolida ombor aytilmagan edi, ma'lumot to'plamida esa ikkita
  ombor bor va standart ombor tanlanmagan. Model «qaysi ombordan?» deb SO'RADI —
  bu to'g'ri xatti-harakat (§0.7), lekin test uni nuqson deb belgilardi.
  Savol matniga ombor qo'shildi.
- **`pay-05`** «Oltin Vodiy 2 mln to'ladi» — reja buni «yo'nalish noaniq» misoli
  deb bergan, lekin o'zbekchada «X to'ladi» yo'nalishni AYON qiladi (pul bizga
  tushdi) va model uni to'g'ri aniqladi. Haqiqiy noaniqlik boshqacha yangraydi:
  matn «Oltin Vodiy bilan 2 mln to'lov bo'ldi» ga o'zgartirildi.

  ⚠️ Xavfsizlik qoidasi O'ZGARMADI: yo'nalish hech qachon QARZ BELGISIDAN
  chiqarilmaydi va yuk tarkibida oshkora ketadi (`AiDraftTests` darvozasi).
  O'zgargani — modeldan gapdagi aniq ma'noni ham tushunmaslikni talab qilgan
  sun'iy savol.

### Baholashdagi ikki tuzatish

- Noaniq holatlarda ANIQ tool talab qilinmaydi: «Plombir qancha qoldi?» savoliga
  model `stock_query` emas, `find_product` chaqirib uch nomzodni ko'rishi va
  SHUNDAN keyin so'rashi bir xil darajada to'g'ri. O'lchanadigan narsa — taxmin
  qilmagani, tool tanlovi emas.
- Xato holatda hisobotga javob PARCHASI yoziladi: «javobda yo'q: 12 000» degan
  qator sababni aytmaydi.

### Ochiq

- To'liq 77 savollik yurish (≈ $2) — byudjet foydalanuvchining prod tekshiruvi
  uchun saqlandi.
- Token-token oqim — hali qo'shilmagan.

---

## A0–A3 prod deploy — 2026-09-15 19:03

`docs/DEPLOY.md` (F7 usuli) bo'yicha: image lokalda qurildi, `docker save` bilan
ko'chirildi, serverda faqat `docker load` + `up --no-build`.

| Qadam | Natija |
|---|---|
| Zaxira | `/opt/agentics/backups/wms-ai-20260915-1901.dump` (238K) |
| Eski image teglari | `wms-api:pre-20260915-1902`, `wms-web:pre-20260915-1903` |
| Compose (§4) | `docker-compose.prod.yml` ko'chirildi — unda `Ai__*` bloki bor |
| Migratsiya | `F10_AiFoundation` qo'llandi; 4 ta `ai_*` jadval yaratildi |
| `wms-api` | healthy, `/health` → 200 |
| `wms-web` | healthy, `/login` → 200, yangi bundle efirda |
| `POST /api/ai/chat` | 401 (tokensiz — to'g'ri) |
| API jurnalida xato | 0 |
| Xotira | 1553 MB band / 1407 MB bo'sh |

### Sozlamalar

Prod `.env` ga AI bloki qo'shildi (eski nusxa `.env.bak-20260915-19xx`):
`AI_API_KEY`, `AI_MODEL=claude-sonnet-5`, `AI_EFFORT=low`, **`AI_DAILY_USD_CAP=5`**.

⚠️ Kunlik shift prod'da ataylab **5** (lokalda 10): hozircha bitta $10 balans bor
va u ham foydalanuvchining qo'l tekshiruvi uchun kerak.

⚠️ **Dev va prod BIR XIL kalitda ishlayapti** — reja §A0 buni taqiqlaydi (dev
tajribasi prod hisobini yeydi, kalitni almashtirish ikkala muhitni birdan uzadi).
Alohida prod kaliti olinishi kerak.

### `ai.chat` kimga yoqildi

| Tenant | Holat |
|---|---|
| `demo` (Demo tashkilot) | ✅ yoqildi |
| `un-angren-1` (haqiqiy mijoz) | ❌ **TEGILMADI** |

Haqiqiy mijozga hali brauzerda ko'z bilan tekshirilmagan imkoniyat yoqilmadi —
u pul turadi va mijoz undan xabarsiz. Yoqish qarori foydalanuvchiniki (Console
orqali yoki `tenant_feature` override'i bilan).

### Deploy yo'lidagi to'siq (hal bo'ldi)

Dastlab na SSH, na `docker build` ishlamasdi: konteynerlarda DNS yechilmasdi va
SSH banner kelmasdi (github.com:22 ham). Sabab shu mashinadagi xavfsizlik dasturi
edi — foydalanuvchi uni sozlagach ikkalasi ham tuzaldi. Serverda bunday muammo
yo'q (`DNS-OK`).

### Ochiq

- Brauzerda va botda qo'l tekshiruvi — foydalanuvchida.
- To'liq 77 savollik jonli eval (≈ $2) yurgizilmagan.
- Alohida prod API kaliti.
