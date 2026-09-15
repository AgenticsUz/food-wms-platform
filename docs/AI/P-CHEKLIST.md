# F10 — AI'dan OLDINGI ishlar (P bosqichi) — bajarish varag'i

> Sana: 2026-09-14. Bu — `WMS-AI-REJA.md` P bosqichining ISH VARAG'I: tartib,
> ustuvorlik va holat shu yerda yuritiladi; har ishning batafsil tavsifi va qabul
> mezoni REJA'ning ko'rsatilgan bo'limida. Sessiya ishni tugatgach shu faylda
> holatni yangilaydi (✅/⏳/❌ + sana + commit) va katta xulosalarni
> `docs/F10-HISOBOT.md` ga yozadi.
>
> **Tartib qoidasi:** 1-BLOK bajarilmagunicha A0/A1 boshlanmaydi. 2-BLOK A1 bilan
> parallel yoki A3'dan oldin. 3-BLOK ixtiyoriy/parallel — AI'ni bloklamaydi.

---

## 1-BLOK — AI'ni BLOKLAYDI (avval shular)

### 1.1 P0 dumlari (REJA §P0)

| # | Ish | Holat |
|---|---|---|
| 1 | ~~Prod deploy~~ | ✅ 2026-09-15 09:15 — T2 + P1/P2.1 birga (`8da67bc`), zaxira `wms-20260915-0911.dump`, `F10_NameSearch` qo'llandi |
| 2 | ~~§3 SQL tekshiruvi~~ | ✅ 2026-09-14 — sabab aniqlandi (quyida) |
| 3 | ~~FEFO/o'chirilgan partiya tuzatishi~~ | ✅ 2026-09-14 (`StockAllocator`, darvoza testlari bilan) |
| 4 | Bot tokenini BotFather'da `/revoke`, yangi token prod `wms.env` ga (token suhbatlarda ko'ringan — xavfsizlik) | ⏳ **foydalanuvchida** |
| 5 | ~~Platforma + WMS push~~ | ✅ 2026-09-14 (`8fea6fe`, `096f2be`) |
| 6 | ~~T2 kod tuzatishi~~ | ✅ 2026-09-14 (`096f2be`) |

**§3 natijasi (prod, faqat o'qish):** hujjat — Pending chiqim, manba **Sklad1**,
mahsulot **Snikers**, 34 dona. O'sha mahsulot bo'yicha qoldiq qatori **0 ta**,
Sklad1 da umuman qoldiq yo'q, butun bazada o'chirilgan partiyali qator ham **0 ta**.
Ya'ni ikkala gumon ham tasdiqlanmadi: tovar hech qachon kirim qilinmagan va xato
MAZMUNAN to'g'ri edi; nuqson faqat UX'da bo'lgan (T2 da tuzatilgan). Batafsil —
`docs/F10-HISOBOT.md`.

**O'chirilgan partiya qarori:** prod'da bunday qator yo'q, lekin nuqson latent
turardi (`GetStockAsync` ko'rsatadi, `StockAllocator` yashirardi). Haqiqat — qoldiq
qatorining o'zi; `AvailableRows` endi yumshoq-o'chirish filtrini o'chiradi va faqat
qoldiq qatorining O'Z `is_deleted` ini tekshiradi. Darvoza:
`tests/WMS.Tests/Catalog/StockAllocatorTests.cs`.

### 1.2 P1 — Backend test poydevori (REJA §P1) — ENG KATTA BO'SHLIQ

Hozir backend'da 0 ta test (faqat 5 frontend spec). AI hujjat yozishidan oldin
`TransferService`/FEFO testli bo'lishi shart.

| # | Ish | Holat |
|---|---|---|
| 1 | `tests/WMS.Tests` (xUnit v3 + Testcontainers PostgreSQL, RLS haqiqiy bazada) + `AgenticsWms.slnx` ga ulash | ✅ 2026-09-14 (`17217a6`) |
| 2 | Tenant konteksti test-yordamchisi (`WmsTenantScope`, `TestData`) | ✅ (`17217a6`) |
| 3 | `TransferService` testlari (7 stsenariy) | ✅ (`a490f42`) |
| 4 | `GetAvailableAsync` = `DeductFefoAsync` shart bir xilligi testi | ✅ (`a490f42`) |
| 5 | `scripts/run-tests.sh` + CI `integration` job'i | ✅ |

Qabul: **44 test yashil** (≥ 15 talab qilingan edi); **3 darvoza tuzatishsiz qizil
berishi tekshirilgan** — vaqtinchalik `git worktree` da eski kodga qarshi yurgizib:
«o'chirilgan partiyali qator FEFO'da ko'rinadi», «ikki yuza bir xil javob beradi»
(ikkalasi FEFO tuzatishidan oldingi kodda qizil) va «qoldiq yetmasa YARATISHDA
xato» (yaratishdagi tekshiruv olib tashlanganda qizil).

⚠️ Ruxsat (§P1 №7) servis darajasida EMAS, controllerda tekshiriladi — shuning
uchun 403 testi o'rniga statik darvoza: `WmsSystemRoles` to'plamlari va
`[RequirePermission]` atributlari refleksiya bilan qo'riqlanadi.

### 1.3 P2.1 — Nom qidiruvi (REJA §P2.1) — A1 tool'lari uchun shart

| # | Ish | Holat |
|---|---|---|
| 1 | Migratsiya `F10_NameSearch`: `pg_trgm`, `name_search` ustunlari, GIN trigram indeks, backfill | ✅ 2026-09-14 (`cc51818`) |
| 2 | `ISearchService` (product/counterparty/warehouse, o'xshashlik bali bilan nomzodlar) | ✅ (`cc51818`) |
| 3 | UI qidiruvlari serverga o'tdi (300 ms debounce; mijozdagi `includes` o'chdi) | ✅ (`cc51818`) |

Qabul: ✅ «Snikers»/«snickers»/«Сникерс» — bitta mahsulot birinchi o'rinda;
«Plombir» — 3 nomzod (testlar bilan qo'riqlanadi). Lokal stendda migratsiya
qo'llandi va backfill hamma qatorni to'ldirdi (bo'sh `name_search` — 0 ta).

⚠️ GIN indeks so'rovda ATAYLAB ishlatilmayapti (indeksli `<%` seans chegarasiga
bo'ysunadi va «snikers»↔«snickers» ni kesardi) — katalog o'sganda
`SET LOCAL pg_trgm.word_similarity_threshold` bilan indeksga o'tiladi.
⚠️ Transliteratsiya o'zbekcha (ж→j); ruscha `zh` yozilsa bal past bo'lishi mumkin.

---

## 2-BLOK — A1 bilan parallel / A3'dan OLDIN (REJA §P2.2–P2.9)

| # | Ish | Kimga kerak | Holat |
|---|---|---|---|
| 1 | P2.2 Oxirgi narx (`IPricingService`) | A1 `last_price`, A3 qoralama | ✅ 2026-09-15 |
| 2 | P2.3 Hujjat sanasi (`DocumentDate` + `documents.backdate`) | A3 «kecha kelgan kirim» | ✅ 2026-09-15 |
| 3 | P2.4 Qisqa hujjat raqami (tenant hisoblagichi) | A3/UI qulayligi | ✅ 2026-09-15 |
| 4 | P2.5 Partiya tannarxi (`Batch.UnitCost`, `TransferItem.UnitCost`, ishlab chiqarish sarfi) | A4 foyda hisoboti | ✅ 2026-09-15 |
| 5 | P2.6 Standart ombor (tenant + xodim override'i) | AI qayta so'rashini kamaytiradi | ✅ 2026-09-15 |
| 6 | P2.7 Qadoq — **Variant A** (`PackSize` + `PackUnit`) | A3 «50 quti» | ✅ 2026-09-15 (backend; konversiya formada) |
| 7 | P2.8 To'lov sanasi, yo'nalishi va manbasi | A3.2 «kecha to'ladi» | ✅ 2026-09-15 |
| 8 | P2.9 To'lovni qaytarish (storno) | A3.2 sharti | ✅ 2026-09-15 |

**Nima qilindi (qisqacha):**

- **Hujjat sanasi** butun tizimda bitta ma'no oldi: ilgari ro'yxat `CreatedAt`, hisobot
  `ConfirmedAt`, ishlab chiqarish yana `CreatedAt` bo'yicha ishlardi va bir xil davr uch xil
  natija berardi. Endi `Transfer.DocumentDate` va `PaymentHistory.DocumentDate`; orqaga sana
  — `documents.backdate` ruxsati bilan, kelajak sana hech kimga.
- **Qisqa raqam**: `tenant_counter` jadvali (`UPDATE … RETURNING`), tenant ichida noyob;
  20 parallel yaratish testi 20 ta noyob raqam beradi.
- **Tannarx**: kirimda partiyaga narx nusxalanadi, chiqimda FEFO tanlagan partiyalarning
  og'irlangan o'rtachasi hujjat qatoriga yoziladi, ishlab chiqarishda bosqich sarfi
  (`StageExecution.MaterialCost`) yig'ilib tayyor partiyaga tushadi. Tannarxi NOMA'LUM
  qatorlar hisobotda alohida ko'rsatiladi — foyda soxta katta ko'rinmasin.
- **To'lov**: yo'nalish endi SAQLANADI va nol balansda TAXMIN QILINMAYDI (ilgari qarz
  belgisidan chiqarilardi — nol balansda summa teskari tomonga yozilib, xatoni ikki barobar
  qilardi). Storno: to'lov o'chirilmaydi, teskari yozuv qo'shiladi; ikki marta qaytarib
  bo'lmaydi; mijozga Telegram orqali xabar ketadi.

**Yo'l-yo'lakay tuzatilgan eski nuqsonlar:**

- Telegram kunlik xulosa xizmati bir kunda qayta ko'tarilganda `23505` (dedup kaliti) bilan
  yiqilib, butun tenant aylanishini uzardi — endi kalitlar oldindan o'qiladi va 23505 zaxira
  to'r sifatida ushlanadi (`PostgresErrors`). Marshrut va mijoz xabarlarida ham xuddi shu.

---

## 3-BLOK — Parallel / bloklamaydi

| # | Ish | Holat |
|---|---|---|
| 1 | Kabinet ekranlarini mijoz/agent sifatida brauzerda ko'rish — foydalanuvchi (Kaspersky sababli lokal brauzer tekshiruvi foydalanuvchida) | ⏳ |
| 2 | Qarz #17: WMS konteyner nginx'iga CSP (Console/HRM naqshi) | ⏳ |
| 3 | P3 skaner maydoni va etiketka (REJA §P3) — AI'ga bog'liq emas | ⏳ |

---

## Yakuniy darvoza (A0 ga o'tishdan oldin)

- [x] 1-BLOK: P0 №2–3, P1, P2.1 ✅ — qolgani: **№1 deploy** (ertalab) va
      **№4 bot tokeni** (BotFather'da faqat foydalanuvchi qila oladi)
- [x] `dotnet build` 0/0, `run-tests.sh` — **44 test yashil**, `wms-web`
      lint/test/build yashil (kesh'siz tekshirildi)
- [x] Prod deploy qilingan (2026-09-15 09:15) — brauzer tekshiruvi foydalanuvchida
- [x] P2.7 qadoq qarori: **Variant A** (`PackSize` + `PackUnit`)
- [x] Shu fayl va `docs/F10-HISOBOT.md` yangilangan

Lokal stend (2026-09-14): image'lar qayta qurildi, `wms-migrator` `F10_NameSearch`
ni qo'lladi, `wms-api`/`wms-web` healthy. Brauzer tekshiruvi foydalanuvchida
(Kaspersky interstitial'i sababli).

---

## A0 — AI poydevori (REJA §A0) — ✅ 2026-09-15

| # | Ish | Holat |
|---|---|---|
| 1 | `Anthropic` NuGet (rasmiy C# SDK), aniq versiya `12.47.0` | ✅ |
| 2 | `ILlmClient` + `LlmContracts` (Application), `AnthropicLlmClient` (Infrastructure) | ✅ |
| 3 | `AddAiModule()` → `WmsModules.cs`; `AiOptions` binding'i | ✅ |
| 4 | Tool registri: `IAiTool`, `AiToolContext`, `IAiToolRegistry` + `AiToolRegistry` | ✅ |
| 5 | JSON sxema DTO'dan (`AiToolSchema`, `JsonSchemaExporter`) | ✅ |
| 6 | Jadvallar + migratsiya `F10_AiFoundation` | ✅ |
| 7 | Metering: `IAiMetering`/`AiMetering`, narx jadvali `AiPricing` | ✅ |
| 8 | Limitlar: feature `ai.chat`, plan kvotasi, kunlik dollar shifti | ✅ |
| 9 | Audit: `Transfer.AiConversationId`, `PaymentHistory.AiConversationId` | ✅ |
| 10 | Konfig: `appsettings.json`, compose (dev+prod), `docker/.env.example` | ✅ |
| 11 | Xato kodlari (`ai_*`) + uz/ru tarjima + middleware | ✅ |
| 12 | Testlar: `FakeLlmClient`, registr filtri, sxema, metering | ✅ **18 yangi test** |

**Qabul mezoni:**

- [x] `dotnet build` — 0 xato / 0 ogohlantirish
- [x] `bash scripts/run-tests.sh` — **102 test yashil** (84 → 102)
- [x] `Ai__ApiKey` bo'sh — modul o'chiq, tizim ishlayveradi (test bilan qo'riqlanadi)
- [x] `ai.chat` bazaviy katalogda (`BaseCatalogSeeder`), demo tenantga yoqilgan (`DemoSeeder`)
- [x] `F10_AiFoundation` haqiqiy Postgres'da qo'llandi; RLS darvozasi (`RlsPolicyTests`)
      yangi tenant jadvallarini o'z-o'zidan qamrab oldi

**Muhim qarorlar:**

- **`ai.chat` hech bir planga KIRMAYDI va sukuti o'chiq.** Boshqa feature'lar bepul
  yuzalar — ular uchun «sukut bo'yicha yoqiq» to'g'ri; AI esa har chaqiriqda pul
  turadi, shuning uchun uni Console operatori tenantga oshkora yoqadi.
- **Ikki qatlamli sarf hisobi.** `ai_usage` — tenant bo'yicha (RLS), `ai_daily_cost` —
  platforma bo'yicha (tenant ustuni yo'q). Sabab: kunlik dollar shifti HAMMA tenant
  yig'indisiga qaraydi va RLS ostidagi jadvaldan bunday yig'indi olib bo'lmaydi.
- **Narx kodda emas, konfigda** (`Ai:Pricing:{model}`) — tarif o'zgarganda deploy
  kerak emas. Jadvalda model topilmasa sarf BARIBIR yoziladi (token — haqiqat), dollar
  esa 0 bo'ladi va logga ogohlantirish chiqadi: jim nol «AI bepul» degan xato xulosa.
- **Rad javoblari uch xil:** `ai_disabled` 403 (kalit/feature), `ai_quota_exceeded` 402
  («planni ko'taring» oqimi), `ai_unavailable` 503 (provayder yoki kunlik shift —
  aybdor tenant emas).

**Yo'l-yo'lakay tuzatilgan nuqsonlar (A0 kodi hali commit qilinmagan edi):**

- `AiToolSchema.For<T>()` ildiz sxemasini `["object","null"]` deb chiqarardi
  (`JsonSchemaExporter` sukuti) va o'z tekshiruvida yiqilardi —
  `TreatNullObliviousAsNonNullable` qo'shildi.
- `AnthropicLlmClient.ParseEffort` `xhigh` ni bilmasdi, `AiOptions` esa uni
  hujjatlashtirgan edi: `Ai__Effort=xhigh` birinchi jonli chaqiriqda yiqilardi.

**A0 dan keyin ochiq:** suhbat tarixini tozalash fon vazifasi (`HistoryRetentionDays`
siyosat qiymati bor, vazifa — A1), `ai_usage` ni Console'da ko'rsatish.

Shundan keyin — `WMS-AI-REJA.md` §A1 (o'quvchi tool'lar, gateway, sinov to'plami, Telegram).
