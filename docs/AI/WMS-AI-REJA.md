# WMS — AI qatlami REJASI (F10)

> Sana: 2026-09-14. Manba: `docs/AI/AI-TAVSIYA.md`, `docs/XATOLAR-2026-09-14.md`,
> `docs/CLAUDE.md`, kod holati `f8-tenant-self-service @ 096f2be` (T2 tuzatilgan).
> Bosqichlar KETMA-KET bajariladi, muddat yo'q. Har bosqich — alohida Claude Code
> sessiyasi; bosqich yakunida `docs/F10-HISOBOT.md` ga qisqa hisobot (nima qilindi,
> qabul mezoni natijasi, ochiq qolganlar). Keyingi bosqichga faqat qabul mezoni
> yashil bo'lsa o'tiladi.

---

## 0. Qat'iy qoidalar (barcha bosqichlar uchun)

1. **Til va runtime:** hammasi .NET 10 + Angular 21, `wms-api` va `wms-web` ichida.
   Python, alohida xizmat, alohida baza, pgvector — YO'Q. (Ovoz, RAG, ML — §K.)
2. **AI foydalanuvchi sessiyasi ichida.** Alohida "super" hisob yo'q. Tool
   bajarilish paytida `WmsPermissions` tekshiriladi, RLS tenant kontekstida ishlaydi.
3. **Xavfsizlik chegarasi — tool registri, prompt emas.** Ruxsatsiz tool modelga
   umuman ko'rsatilmaydi. System prompt qo'shimcha himoya, asosiy emas.
4. **Tool'lar servisni chaqiradi, controller'ni emas.** Yangi biznes-logika
   tool ichida yozilmaydi.
5. **Javob faqat tool natijasidan.** Raqamli savolga tool chaqirilmagan bo'lsa
   javob — "aniqlab bera olmadim".
6. **Yozuvchi amal faqat odam tasdig'i bilan.** AI `CreateAsync` (Pending) gacha
   boradi; `ConfirmAsync` faqat foydalanuvchi tugmasi bilan.
7. **Noaniqlik — savol, taxmin emas.** Mahsulot/kontragent bir nechta topilsa,
   ombor aytilmagan va bir nechta bo'lsa — AI qayta so'raydi.
8. **Har chaqiriq auditda va hisobda** (`ai_usage`): tenant, foydalanuvchi, model,
   token, dollar, tool ro'yxati, davomiylik.
9. **Provayder interfeys ortida** (`ILlmClient`). Gateway Anthropic SDK turlarini
   bilmaydi.
10. **Testlar API'siz yuradi** — LLM javoblari fixture'da (yozib olish / qayta o'ynatish).
11. CLAUDE.md §3 qoidalari (RLS, ikki yuza, `ApiResponse` + `code`, 0 xato/0
    ogohlantirish, o'zbekcha izohlar) o'z kuchida.
12. Branch: `f10-ai` (`f8-tenant-self-service` dan). Commit prefiksi bosqich kodi
    bilan: `p1:`, `a1:` ...

---

## P — POYDEVOR (AI'dan oldin)

> **Ish varag'i — `docs/AI/P-CHEKLIST.md`:** P ishlarining tartibi, ustuvorligi
> (nima AI'ni bloklaydi, nima parallel) va joriy holati o'sha yerda yuritiladi;
> bu bo'limda — tavsif va qabul mezonlari.

### P0 — Ochiq bandlarni yopish

**Ishlar**
- T2 (`096f2be`) prod'ga deploy (F7 usuli, `docs/DEPLOY.md`).
- `XATOLAR-2026-09-14.md` §3 — ikki prod SQL (faqat o'qish) → `01a0964f-…` sababi
  → FEFO/o'chirilgan partiya qarori va (kerak bo'lsa) tuzatish.
- Kabinet ekranlarini mijoz/agent sifatida brauzerda ko'rish; topilgan nuqsonlar.
- ~~Platforma repo push~~ — ✅ bajarildi 2026-09-14 (`8fea6fe`; WMS ham push qilingan).
- Bot tokenini BotFather'da `/revoke`, yangi token `wms.env` ga.
- Qarz #17: WMS konteyner nginx'iga CSP (Console/HRM naqshi).

**Qabul mezoni:** HOLAT/XATOLAR'da WMS bo'yicha ⏳ band qolmaydi; prod curl ro'yxati
(`DEPLOY-SERVER.md` §7) yashil.

### P1 — Backend test poydevori

Hozir backend test loyihasi YO'Q (faqat 5 ta frontend spec). AI hujjat yozishidan
oldin `TransferService` testsiz qolishi mumkin emas.

**Ishlar**
- `tests/WMS.Tests` (xUnit, Testcontainers PostgreSQL — platforma repo naqshi;
  RLS haqiqiy bazada tekshiriladi, SQLite EMAS). `AgenticsWms.slnx` ga ulash.
- Test tenant + tenant konteksti yordamchisi (RLS ostida ishlash uchun).
- `TransferService` testlari:
  - Outgoing: yaratish (Pending) → tasdiqlash → zaxira kamayadi, FEFO partiya tanlanadi
  - Outgoing: qoldiq yetmasa yaratishda xato «kerak N, mavjud M»
  - Incoming: tasdiqlashda partiya yaratiladi (lot, `ShelfLifeDays` dan muddat)
  - Parallel tasdiq: 200 + 409, zaxira BIR marta o'zgaradi (D13)
  - Rad etish: zaxira o'zgarmaydi
  - Agent komissiyasi: `CommissionRecord` noyobligi
  - Ruxsat: `transfers.create` yo'q foydalanuvchi — 403 (servis darajasida)
- `IStockAllocator.GetAvailableAsync` va `DeductFefoAsync` — bir xil shart bilan
  ishlashi (INNER JOIN partiya).
- `bash scripts/run-tests.sh` (platforma naqshi) — `dotnet test` MTP bilan ishlamasa.

**Qabul mezoni:** ≥ 15 test yashil, `bash scripts/run-tests.sh` bilan lokal yuradi
(repo'da CI o'chiq — `deploy.yml`; CI qo'shilsa o'sha skript chaqiriladi);
tuzatishsiz qizil berishi tekshirilgan kamida 3 test (zaxira, 409, ruxsat).

### P2 — Domen bo'shliqlari (AI to'qnashadigan joylar)

Har biri UI uchun ham foyda. Tartib bilan, har biri alohida commit.

**P2.1 — Nom qidiruvi**
- Migratsiya: `pg_trgm`, `unaccent` extension'lari; `product.name`,
  `counterparty.name`, `warehouse.name` ga GIN trigram indeks.
- `ISearchService.FindProductsAsync(query, limit)` /
  `FindCounterpartiesAsync` / `FindWarehousesAsync`: natija — nomzodlar ro'yxati
  o'xshashlik bali bilan. Lotin↔kirill transliteratsiya — ⚠️ `unaccent` buni
  QILMAYDI: normalizatsiya C# tomonda (`name_search` ustuni transliteratsiya
  bilan to'ldiriladi, GIN trigram indeks SHU ustunga; so'rov ham shu funksiyadan
  o'tadi). Tarjima/sinonim (`un`→`мука`) YO'Q — faqat alifbo (`Snikers`/`Сникерс`).
- Mavjud UI qidiruvlari (`products`, `counterparties`) shu servisga o'tadi.
- Qabul: «Snikers», «snickers», «Сникерс» — bitta mahsulot birinchi o'rinda;
  «Plombir» — 3 nomzod qaytadi.

**P2.2 — Oxirgi narx**
- `IPricingService.GetLastPriceAsync(productId, counterpartyId?, type)`:
  oxirgi TASDIQLANGAN hujjatdagi `UnitPrice` — avval shu kontragent bilan, bo'lmasa
  umuman; natijada manba (hujjat, sana, kontragent) ham qaytadi.
- Transfer formasida narx maydoni yonida taklif («oxirgi: 12 000, 10.09, Korzinka»).
- Qabul: test — 3 hujjatdan to'g'risi tanlanadi; kontragent bo'yicha va umumiy.

**P2.3 — Hujjat sanasi**
- `Transfer.DocumentDate` (`CreatedAt` dan farqli), migratsiya + backfill
  ⚠️ backfill servis qatlamida yoki `app_migrator` uchun RLS'ni hisobga olib
  (XATOLAR §9.3 saboq: migratsiya tenant jadvaliga yoza olmaydi).
- `CreateTransferDto.DocumentDate?` (sukut — bugun); orqaga sana ruxsati alohida
  `transfers.backdate` ruxsati bilan; kelajak sana taqiqlanadi.
- Hisobotlar va FEFO `DocumentDate` bo'yicha.
- Qabul: kecha kelgan kirim kechagi sana bilan kiradi va hisobotda kechada turadi.

**P2.4 — Qisqa hujjat raqami**
- `Transfer.Number` (tenant ichida ketma-ket, `int`), `ProductionOrder.Number`.
  Tenant bo'yicha hisoblagich — parallel yaratishda noyoblik (sequence yoki
  `tenant_counter` jadvali + `xmin`).
- UI'da Guid o'rniga raqam; qidiruv raqam bo'yicha.
- Qabul: 20 parallel yaratish — 20 noyob raqam, bo'shliqsiz emas (bo'shliq ruxsat).

**P2.5 — Partiya tannarxi**
- `Batch.UnitCost` (kirimda `UnitPrice` dan), `TransferItem.UnitCost` (chiqimda
  FEFO tanlagan partiyaning tannarxi, nusxa).
- `AnalyticsService` ga `GetProductProfit(from, to, productId?)` — sotuv −
  partiya tannarxi.
- Qabul: test — ikki partiya turli tannarxda, FEFO birinchisini oladi, foyda to'g'ri.

**P2.6 — Standart ombor**
- Tenant sozlamasi: standart xomashyo/tayyor mahsulot ombori; foydalanuvchi
  profilida ixtiyoriy override.
- Transfer formasida sukut qiymat.
- Qabul: bitta omborli tenantda forma omborni so'ramaydi.

**P2.7 — Qadoq (ixtiyoriy, qaror kerak)**
- Variant A: `Product.PackSize` + `PackUnit` (1 quti = N dona) — formada ham,
  AI'da ham konversiya.
- Variant B: qilinmaydi, AI har safar so'raydi.
- Qaror P2 boshida foydalanuvchi bilan.

**P2.8 — To'lov sanasi va manbasi (A3.2 uchun SHART)**
- Hozir `PaymentHistory.PaidAt = DateTime.UtcNow` — QOTIRILGAN, ya'ni «kecha
  to'lagan edi» ni tizim yoza olmaydi. `PaymentHistory.DocumentDate` qo'shiladi
  (P2.3 bilan bir xil qoida: kelajak sana taqiq, orqaga sana alohida ruxsat bilan).
  ⚠️ P2.3 dagi `transfers.backdate` ruxsati `documents.backdate` deb nomlanadi va
  hujjat ham, to'lov ham shu ruxsatga bog'lanadi (ikkita deyarli bir xil ruxsat
  chalkashlikdan boshqa narsa bermaydi; P2.3 hali yozilmagani uchun nom bepul).
- `PaymentHistory.Source` (`ui` / `telegram` / `ai`) + `AiConversationId?` —
  `Transfer.Source` bilan bir xil naqsh (A0), «buni AI yozganmi» savoli uchun.
- Qabul: kechagi sana bilan kiritilgan to'lov hisobotda KECHAGI kunda turadi;
  `documents.backdate` yo'q foydalanuvchiga orqaga sana 403.

**P2.9 — To'lovni tuzatish (A3.2 uchun SHART)**
- Bugun to'lovni O'CHIRISH ham, QAYTARISH ham yo'q (`FinanceController` da faqat
  `POST payments`; `DELETE` faqat `transactions` da). Ya'ni xato kiritilgan summa
  qarz balansida abadiy qolib ketadi — buni AI kiritganda muammo ikki barobar.
- `IFinanceService.ReversePaymentAsync(paymentId, reason)`: asl yozuv O'CHMAYDI,
  teskari yo'nalishli yangi `PaymentHistory` yoziladi (`ReversalOfId`, sabab bilan),
  qarz balansi qaytariladi. Bir to'lov IKKI marta qaytarilmaydi (`ReversalOfId`
  bo'yicha noyob indeks). Ruxsat — `finance.manage`.
- ⚠️ Mijozga xabar: `Direction = In` to'lovda Telegram orqali «to'lovingiz qabul
  qilindi» ketadi (`TelegramPartnerNotifier`), shuning uchun qaytarilganda ham
  xabar ketishi kerak — aks holda mijozda noto'g'ri balans qoladi.
- Qabul: xato to'lov qaytarilgach qarz AVVALGI qiymatiga qaytadi; ikkinchi
  qaytarish urinishi 409; ikkala yozuv ham tarixda ko'rinadi.

**P2 qabul mezoni:** build 0/0, P1 testlari + yangi testlar yashil, `wms-web`
lint/test/build yashil, prod deploy, brauzerda tekshirilgan.

### P3 — Skaner maydoni va etiketka (AI'ga bog'liq emas, P2 bilan parallel bo'lishi mumkin)

- Transfer formasida «skaner rejimi»: fokusdagi maydon, HID skaner kodi → `by-barcode`
  → qator qo'shiladi/miqdor +1 → maydon tozalanadi.
- Etiketka: kirim tasdiqlangach partiya uchun PDF (Code128 `LotNumber`, mahsulot,
  muddat, miqdor) — mavjud PDF brendlash naqshi (`Reports`).
- Telefon kamerasi (PWA, `BarcodeDetector`/ZXing) — keyingi iteratsiya.
- Qabul: skaner bilan 10 qatorli kirim qo'lsiz kiritiladi; etiketka chop etiladi.

---

## A — AI QATLAMI

### A0 — AI poydevori (hech narsa "gaplashmaydi" hali)

**Ishlar**
- `Directory.Packages.props`: rasmiy `Anthropic` NuGet paketi (Anthropic C# SDK),
  ANIQ versiya qadaladi — «10+» kabi taxminiy emas, o'rnatishda mavjud oxirgi
  barqaror versiya yoziladi.
- Modul: `src/WMS.Infrastructure/Services/Ai/`, `AddAiModule()` `WmsModules.cs` da.
- `ILlmClient` (Application): `CompleteAsync(request) → response` — xabarlar, tool
  ta'riflari, tool chaqiriqlari, usage. `AnthropicLlmClient` (Infrastructure) —
  yagona SDK'ni biladigan joy. Prompt keshlash: system + tool'lar barqaror prefiks.
- Tool registri: `IAiTool { Code, Description, JsonSchema, PermissionCode,
  FeatureCode?, ExecuteAsync(args, ctx) }`. Registr foydalanuvchi ruxsatlari va
  tenant feature'lariga qarab **filtrlanadi**, keyin modelga beriladi.
  JSON sxema — `JsonSchemaExporter` bilan DTO'dan.
- Jadvallar (`TenantEntity`, RLS): `ai_conversation` (kanal, foydalanuvchi,
  yaratilgan, oxirgi faollik), `ai_message` (rol, matn, tool chaqiriqlari JSON,
  token), `ai_usage` (tenant, kun, so'rovlar, kirish/chiqish/kesh token, USD).
  Migratsiya `F10_AiFoundation`.
- Metering: har so'rovdan keyin `ai_usage` yangilanadi; narx jadvali konfigda
  (`Ai:Pricing:{model}`), kodga qotirilmaydi.
- Limitlar: plan feature `ai.chat` + `ai.monthly_requests` (kvota); platforma
  darajasida `Ai:DailyUsdCap` — oshsa `ai_unavailable`. Console'dan feature
  o'chirish = to'liq o'chirgich.
- Audit: `Transfer.Source` (`ui` / `telegram` / `ai`) + `AiConversationId?` —
  A3 uchun oldindan.
- Konfig: `Ai__ApiKey`, `Ai__Model` (sukut `claude-sonnet-5`), `Ai__RouterModel`
  (A5 uchun, hozir bo'sh), `Ai__DailyUsdCap`, `Ai__MaxOutputTokens`;
  `docker/.env.example` ga izoh bilan. Dev/prod alohida kalit.
- Xato kodlari: `ai_disabled`, `ai_quota_exceeded`, `ai_unavailable`,
  `ai_tool_forbidden` — `Translations.cs` (uz/ru).
- Test: `FakeLlmClient` (fixture'dan javob), registr filtrlanishi testi
  (`viewer` ga yozuvchi tool ko'rinmaydi), metering testi (usage yoziladi,
  kvota tugasa `ai_quota_exceeded`).

**Qabul mezoni:** build 0/0; testlar yashil; `Ai__ApiKey` bo'sh — modul o'chiq,
tizim ishlayveradi; feature `ai.chat` bazaviy katalogda (`BaseCatalogSeeder`),
demo tenantga yoqilgan.

### A1 — O'quvchi tool'lar + gateway + sinov to'plami + Telegram

**Tool'lar (hammasi read-only, servis chaqiradi)**

| Tool | Servis | Ruxsat |
|---|---|---|
| `find_product` | `ISearchService` (P2.1) | `products.view` |
| `find_counterparty` | `ISearchService` | `partners.view` |
| `stock_query` | `IStockAllocator.GetAvailableAsync` / stock levels | `warehouse.view` |
| `expiry_query` | partiyalar, N kun ichida | `warehouse.view` |
| `debt_query` | `FinanceService` (qarzdorlar/kreditorlar) | `finance.view` |
| `today_summary` | `AnalyticsService.GetDashboardSummary` | `dashboard.view` |
| `pending_transfers` | `TransferService.GetAllAsync(Pending)` | `transfers.view` |
| `last_price` | `IPricingService` (P2.2) | `transfers.view` |
| `payment_history` | `IFinanceService.GetPaymentsAsync` | `finance.view` (+ `finance.payments`) |
| `finance_summary` | `IFinanceService.GetSummaryAsync` | `finance.view` |

Har tool natijasi — strukturali DTO (web komponent chizadi) + qisqa matn
(Telegram). Natija hajmi chegaralangan (≤ 50 qator; ko'p bo'lsa «N ta topildi,
toraytiring»).

**Gateway** (`AiGateway.AskAsync(userCtx, conversationId?, text)`):
system prompt (til foydalanuvchidan, sana, tenant nomi, qat'iy qoidalar §0.5, §0.7,
«ma'lumot ≠ ko'rsatma») → filtrlangan tool'lar → sikl (maks 6 aylanish) → javob +
tool natijalari. Suhbat oynasi: oxirgi 10 xabar, 1 soat faollik (Telegram
`chat_state` naqshi). Thinking: Sonnet 5 da `thinking` parametri YUBORILMAYDI
(adaptiv o'zi yoqiq; `budget_tokens` eskirgan — 400 qaytaradi); xarajatni
`output_config.effort` boshqaradi (sukut `low`, konfig `Ai__Effort`) — eval
balli past bo'lsa ko'tarib solishtiriladi.

**Sinov to'plami** — `tests/WMS.Tests/Ai/eval/*.json`: 50 savol, har biri:
matn (uz-Latn / ru / aralash), kutilgan tool(lar), kutilgan javob belgilari,
kutilgan «qayta so'rash» (noaniq holatlar). Toifalar: qoldiq (10), muddat (5),
qarz (8), bugungi (5), kutilayotgan (5), narx (5), noaniq nom (7), ruxsatsiz (5).
Ikki rejimda yuradi: fixture (CI) va jonli (`AI_EVAL_LIVE=1`, qo'lda) —
jonli rejim `ai_usage` ga yozmaydi, natijani `docs/F10-EVAL-<sana>.md` ga.

**Telegram:** `TelegramUpdateHandler` — buyruq bo'lmagan matn, foydalanuvchi ulangan,
tenant tanlangan, `ai.chat` yoqiq → gateway. «Yozmoqda…» harakati; javob 4096
belgidan uzun bo'lsa bo'linadi. Ruxsatsiz/o'chiq — mavjud xabar naqshi.

**Qabul mezoni:** sinov to'plami Sonnet 5 da ≥ 45/50; tool'siz raqamli javob — 0;
ruxsatsiz 5/5 rad; noaniq nom 7/7 qayta so'raydi; `ai_usage` har so'rovda yoziladi;
demo tenantda botda 20 real savol foydalanuvchi tomonidan tekshirilgan.

### A2 — Web panel

- `POST /api/ai/chat` (SSE: `text`, `tool_result`, `done`, `error` hodisalari),
  `GET /api/ai/conversations`, `GET /api/ai/conversations/{id}`.
- `wms-web`: global drawer (shell), Signals store, `fetch` + `ReadableStream`
  (EventSource EMAS — Authorization sarlavhasi kerak). Kontekst: joriy sahifa va
  tanlangan ombor gateway'ga uzatiladi (system promptning o'zgaruvchan qismi).
- Tool natijalari komponentlar bilan: `stock_query` → jadval, `debt_query` →
  jadval + kontragent havolasi, `pending_transfers` → hujjat havolalari.
- Kabinet (`/portal`): `client`/`agent` roli uchun alohida tool to'plami
  (`PortalService` orqali: `my_debt`, `my_transfers`) — o'sha gateway.
- i18n uz-Latn/ru; ESLint i18n qoidalari.
- Qabul: 10 asosiy savol web'da; streaming ko'rinadi; `viewer` yozuvchi hech narsa
  ko'rmaydi; kabinet mijozi faqat o'z ma'lumotini oladi; lint/test/build yashil.

### A3 — Yozuvchi amallar (odam tasdig'i bilan)

**A3.1 — Hujjat qoralamasi (kirim/chiqim)**

- Tool `draft_transfer(type, counterparty, items[product, qty, price?],
  warehouse?, documentDate?)`: hamma ID'lar P2.1 tool'laridan keladi; narx bo'sh
  bo'lsa `last_price` (chiqim) / `CostPrice` (kirim); zaxira `GetAvailableAsync`
  bilan tekshiriladi; natija — TO'LIQ qoralama (hujjat yaratilmaydi hali).
- Web: qoralama mavjud transfer formasi ko'rinishida, tahrirlanadi, «Yaratish»
  → `TransferService.CreateAsync` (Pending, `Source=ai`) → «Tasdiqlash» → `ConfirmAsync`.
  Telegram: karta + tugmalar (mavjud tugmali tasdiq naqshi, audit bilan).
- Eskirish: qoralama `Confirm` da qayta tekshiriladi (zaxira, narx); tugma idempotent.
- Tool `confirm_transfer` YO'Q — tasdiq faqat UI/bot tugmasi.
- Sinov to'plamiga 15 savol: «A mijozga 50 quti Snickers, 20 quti Plombir,
  oxirgi narx bilan», «B dan 2,5 tonna shakar keldi», «kecha kelgan 300 kg sut»,
  noaniq holatlar (ombor 2 ta, mahsulot 3 nomzod, qoldiq yetmaydi).
- Qabul: 15/15 — to'g'ri qoralama yoki to'g'ri savol; AI hech qachon `Confirm`
  chaqirmaydi (test); `Source=ai` va suhbat ID hujjatda; audit yozuvi.

**A3.2 — Moliyaviy oldi-berdi (to'lov qoralamasi)**

> Kontekst (foydalanuvchi, 2026-09-15): bank yoki 1C integratsiyasi YO'Q va
> rejalashtirilmagan — ruxsat olinsa §K ga o'tadi. Tizim summani shunchaki RAQAM
> sifatida qabul qiladi, hisobot uchun. Shuning uchun AI hech qachon «pul o'tdi»
> demaydi, faqat «yozuv kiritildi» deydi; to'lov tasdig'i, bank kafolati yoki
> hisobvaraq qoldig'i haqida gapirish TAQIQ.

**Tool `draft_payment(counterparty, amount, direction, method?, note?, documentDate?)`**
- «A mijoz 2 000 000 berdi, qarzidan yop» → `direction = In` (qarz kamayadi).
  «B ta'minotchiga 5 000 000 berdik» → `direction = Out`.
- ⚠️ **`direction` MAJBURIY va hech qachon taxmin qilinmaydi.** `CreatePaymentDto`
  da u ixtiyoriy va bo'sh bo'lsa QARZ BELGISIDAN chiqariladi
  (`FinanceService.CreatePaymentAsync`) — bu odam uchun qulay, AI uchun xavfli:
  balans nolga yaqin yoki teskari bo'lganda summa noto'g'ri tomonga yozilib,
  ikki barobar xato beradi. Gapdan yo'nalish aniq bo'lmasa AI QAYTA SO'RAYDI.
- `counterpartyId` faqat `find_counterparty` dan; bir nechta nomzod bo'lsa —
  savol (§0.7). Summa > 0; usul aytilmasa `Cash` EMAS, savol yoki tenant sukuti.
- Natija — TO'LIQ qoralama va **balans oldi/keyin**: «Korzinka: qarz 5 400 000 →
  to'lovdan keyin 3 400 000». Hujjat YOZILMAYDI.
- Tasdiq: web'da mavjud to'lov formasi ko'rinishida, Telegram'da karta + tugma;
  tugma `IFinanceService.CreatePaymentAsync` ni chaqiradi. `confirm_payment`
  tool'i YO'Q (§0.6).
- ⚠️ `Direction = In` tasdiqlanganda mijozga Telegram'ga «to'lovingiz qabul
  qilindi + qoldiq balans» ketadi. Ya'ni xato tasdiq DARHOL mijozga ko'rinadi —
  shuning uchun tugma matnida summa, yo'nalish va kontragent to'liq yoziladi va
  tuzatish yo'li (P2.9) A3.2 dan OLDIN tayyor bo'lishi shart.
- Bog'liqlik: P2.8 (sana va `Source`), P2.9 (qaytarish), P2.1 (kontragent qidiruvi).

**Tool `draft_transaction(type, amount, description, documentDate?, counterparty?)`**
(kontragentga bog'liq bo'lmagan kirim/chiqim — ish haqi, yoqilg'i, ijara;
`FinanceService.CreateTransactionAsync`, feature `finance.transactions`).
Qarz balansiga TEGMAYDI — AI javobida shu farq aniq aytiladi. Ixtiyoriy: A3.2
yashil bo'lgandan keyin.

**Sinov to'plamiga 12 savol:** «A mijoz 2 mln berdi, qarzidan yop»,
«B ga 5 mln berdik», «kecha 1 mln to'ladi» (orqaga sana), «Korzinkadan pul
keldi» (summa yo'q → savol), «to'ladi» (yo'nalish noaniq → savol), «qarzini
yopdi» (to'liq summa → joriy qarzni o'qib qo'yadi), ruxsatsiz foydalanuvchi
(`finance.manage` yo'q → tool ko'rinmaydi), xato tasdiqdan keyin «bekor qil»
(qaytarish yo'li), «hisobvaraqda pul bormi» (tizim bilmaydi → rad javob).

**Qabul mezoni:** 12/12 — to'g'ri qoralama yoki to'g'ri savol; AI hech qachon
`CreatePaymentAsync` ni O'ZI chaqirmaydi (test); yo'nalish taxmin qilingan holat
0 ta; `Source = ai` va suhbat ID yozuvda; bank/hisobvaraq haqidagi savolga
«tizimda haqiqiy tranzaksiya yo'q» deb javob beradi.

### A4 — Hisobot va anomaliya tool'lari

- `AnalyticsService` metodlari tool sifatida: `income_expense`, `top_debtors`,
  `waste_by_stage`, `shift_efficiency`, `monthly_comparison`, `product_profit`
  (P2.5) — parametrlar: davr, mahsulot, ombor.
- `anomaly_scan`: qoidalar (min zaxiradan past, muddati ≤ 7 kun, muddati o'tgan
  qarz, smena reja −15%, chiqindi normadan yuqori) — har qoida alohida servis
  so'rovi, natija strukturali. AI faqat jamlaydi va izohlaydi.
- «Excel'ga chiqar» — mavjud `ExportService` tool sifatida, fayl havolasi.
- Qabul: «o'tgan oy A mahsulotdan foyda» → `product_profit` to'g'ri davr bilan;
  «qayerda muammo» → `anomaly_scan`, ro'yxat qoidalar bilan mos; 15 savol to'plamda.

### A5 — Xarajat va model optimallashtirish

- Sinov to'plamini Haiku 4.5 da yurgizish; o'tgan toifalar `Ai__RouterModel` ga
  (toifa aniqlash — birinchi aylanishda arzon model, kerak bo'lsa Sonnet'ga o'tish).
- Kechki xulosa (`TelegramDigestBackgroundService`) — Batch API bilan matnli izoh.
- `ai_usage` bo'yicha Console'da ko'rinish (tenant, oy, USD) — `/admin/v1/ai/usage`.
- Tarif matritsasi: Start — yo'q; Pro — `ai.monthly_requests` = 300; Business —
  1000 + qo'shimcha paket. Narx qoidasi: tarif farqi ≥ tannarx × 3.
- Qabul: bir tenant o'rtacha so'rov narxi hisobotda; Haiku ulushi ≥ 50% sifat
  tushmasdan (to'plam ≥ 45/50 saqlanadi).

---

## K — KELAJAK (rejada, muddatsiz, A1–A5 barqaror ishlagandan keyin)

- **Ovoz (Telegram):** STT API (Google `uz-UZ` yoki mahalliy provayder) →
  matn → o'sha gateway. Qoida: ovozli buyruq avval «Men shunday tushundim: …»
  bilan qaytariladi, keyin oddiy zanjir. Lokal Whisper kerak bo'lsa — faqat
  audio→matn qiladigan kichik konteyner, bazaga tegmaydi.
- **RAG:** qo'llanma 200+ sahifaga chiqsa — `pgvector` + embedding API;
  ungacha `search_docs` = `pg_trgm` matn qidiruvi.
- **Python sidecar qoidasi:** faqat model yurgizish (prognoz, STT, embedding);
  stateless, tenant/ruxsat/baza bilmaydi; C# tool yoki `ILlmClient` sifatida ulaydi.
- **Boshqa mahsulotlar (Wash/HRM):** gateway'ning domen-mustaqil qismi
  (sikl, registr, metering, SSE) `Agentics.Platform.Ai` paketiga — WMS
  tajribasidan keyin.
- **Bank / 1C integratsiyasi:** mijozda 1C yoki bank API'siga ruxsat paydo
  bo'lsa — to'lovlar QO'LDA emas, chiqishdan olinadi (import yoki webhook) va
  `PaymentHistory` ga `ExternalId` bilan tushadi. Ungacha (A3.2 va butun F10
  davomida) summalar faqat qo'lda kiritilgan RAQAM: hisobot uchun, haqiqiy
  tranzaksiya emas.
- **Kamera skaner (PWA)**, tarozi/printer bridge — mijoz so'raganda.

---

## Ilova — konfig va kodlar (yagona ro'yxat)

| Kalit | Sukut | Izoh |
|---|---|---|
| `Ai__ApiKey` | bo'sh | bo'sh → modul o'chiq |
| `Ai__Model` | `claude-sonnet-5` | asosiy model |
| `Ai__RouterModel` | bo'sh | A5: arzon model |
| `Ai__DailyUsdCap` | `10` | platforma to'xtatgichi |
| `Ai__MaxOutputTokens` | `1024` | |
| `Ai__MaxToolRounds` | `6` | |
| `Ai__Effort` | `low` | `output_config.effort`; eval bilan taqqoslab ko'tariladi |
| `Ai:Pricing:{model}` | jadval | USD/1M token, kirish/chiqish/kesh |

Feature'lar: `ai.chat`, `ai.actions` (A3 — hujjat ham, to'lov ham), `ai.reports` (A4).
Xato kodlari: `ai_disabled`, `ai_quota_exceeded`, `ai_unavailable`, `ai_tool_forbidden`.
Ruxsat: AI qatlami (A bosqichlari) YANGI ruxsat qo'shmaydi — tool'lar mavjud
`WmsPermissions` kodlariga bog'lanadi (A3 da `transfers.create`; barcha A1
tool'larining kodlari yuqoridagi jadvalda — hammasi kodda bor, tekshirilgan).
Yangi ruxsatlar FAQAT P bosqichida: `documents.backdate` (P2.3 + P2.8 — hujjat va
to'lovga orqaga sana). A3.2 to'lov tool'lari mavjud `finance.view` / `finance.manage`
kodlariga va `finance.payments` / `finance.transactions` feature'lariga bog'lanadi.
