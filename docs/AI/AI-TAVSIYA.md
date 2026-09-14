# WMS'ga AI (chatbot + function calling) qo'shish — tahlil va tavsiya

> Sana: 2026-09-14. Maqsad: foydalanuvchi erkin matnda so'raydi («Olma qoldig'i
> qancha?», «Kim qarzdor?»), tizim buni tushunib, RAG/function calling orqali
> javob beradi yoki vazifa bajaradi. Avval FAQAT WMS'da sinaladi; o'zini
> oqlagach boshqa mahsulotlarga (wash, hrm) ko'chiriladi.
>
> **Hozirgi holat manbalari** (shu tahlil o'shalarga tayanadi):
> `docs/CLAUDE.md` (arxitektura, qat'iy qoidalar), `../agentics-platform/docs/PLATFORMA-TZ.md`
> (D-qarorlar, F0–F8), `../agentics-platform/docs/HOLAT.md` (platforma holati),
> `docs/TELEGRAM-BOT-TZ.md` (TG1–TG17), `docs/XATOLAR-2026-09-14.md` (oxirgi deploy),
> `docs/CUSTOM_FEATURES.md`, `docs/DEPLOY.md`. Yo'llar wms repo ildizidan.
>
> ⚠️ **Bu — tahlil hujjati. Amaldagi reja — shu papkadagi `WMS-AI-REJA.md` (F10):**
> bosqichlar, qarorlar va qabul mezonlari o'sha yerda; ikkalasi farq qilsa REJA ustun.

---

## 1. Xulosa (bir abzasda)

AI bu loyihaga mos va biz o'ylagandan tayyorroqmiz: Telegram bot (TG1–TG17)
allaqachon «tool katalogi» vazifasini o'taydi — tenant tanlash, RBAC tekshiruvi
va formatlangan javoblar bilan ishlaydigan so'rov buyruqlari bor. AI qatlami shu
servislarni Claude tool use orqali chaqiradi, xolos. Eng arzon birinchi qadam —
**Telegram botga erkin matn yo'nalishi** (buyruq bo'lmagan xabar → AI): yangi
kanal, auth, UI kerak emas. RAG (hujjat qidiruvi) — uchinchi bosqich, chunki WMS
savollarining ko'pi hujjat emas, **bazadan aniq javob** talab qiladi (bu
function calling ishi).

## 2. Inventar — nimalar BOR

| Baza | Qayerda | AI uchun ahamiyati |
|---|---|---|
| Servis qatlami | `WMS.Application`/`WMS.Infrastructure` (`TransferService`, `StockAllocator`, `PortalService`, Analytics) | Tool'lar controller emas, shu servislarni chaqiradi — yangi biznes-logika yozilmaydi |
| Telegram so'rov buyruqlari | `TelegramQueryCommands` (`/qoldiq`, `/muddat`, `/qarz`, `/bugun`, `/kutilmoqda`) | Har biri tayyor tool namunasi: tenant konteksti + bajarilish paytida RBAC + natija formati |
| RLS + RBAC | `TenantEntity`, RLS interceptor, `WmsPermissions` | AI foydalanuvchi kontekstida ishlasa, ko'rmasligi kerak narsani jismonan ko'ra olmaydi |
| Feature/plan tizimi | `TenantState.EnabledFeatures`, `[RequireFeature]` | AI'ni `ai.chat` feature qilib plan'ga bog'lash mumkin — kim uchun yoqilgani Console'dan boshqariladi |
| Tenant chat konteksti | `telegram_chat_state` (bir soatlik tanlov) | AI suhbatida ham xuddi shu naqsh ishlaydi |
| Audit | `Operations/Audit` | AI chaqiriqlarini yozish uchun tayyor joy |
| RAG kontenti | `docs/*.md`, `../docs/qollanma/agentics-wms-qollanma.html` | «Qanday qilaman?» savollari uchun bilim bazasi |
| Infra | .NET 10, PostgreSQL 17, Docker, SSE'ga to'siq yo'q | Yangi runtime kerak emas |

## 3. Inventar — nimalar YO'Q (qo'shiladi)

1. **LLM integratsiyasi** — hech qanday AI paketi yo'q. Anthropic C# SDK
   qo'shiladi (`Directory.Packages.props` orqali, markazlashgan versiya).
2. **AI gateway** — bitta joy: prompt yig'ish, tool-use sikli, xatolik/retry,
   token hisobi. `Operations` yoki alohida `Ai` modul (`AddAiModule()`).
3. **Suhbat jadvallari** — `ai_conversation`, `ai_message` (tenant, RLS ostida,
   `TenantEntity`); Telegram uchun chat_id bog'lanishi.
4. **Tool registry** — tool ta'riflari (nom, tavsif, JSON schema) + har biri
   ruxsat kodiga bog'langan (`WmsPermissions`); ruxsatsiz tool ro'yxatga
   umuman kirmaydi.
5. **SSE streaming endpoint** — web chat uchun (`/api/ai/chat`); Telegram'da
   kerak emas (javob tayyor bo'lgach yuboriladi).
6. **Xarajat nazorati** — tenant bo'yicha kunlik so'rov/token limiti (plan
   feature'i bilan), `usage` maydonlarini logga yozish.
7. **Vector store** — faqat RAG bosqichida: prod `postgres:17-alpine` da
   pgvector yo'q → `pgvector/pgvector:pg17` image'ga o'tish (dev/prod compose).
8. **Chat UI** — Angular'da yon panel (web bosqichida).
9. **Sirlar** — `ANTHROPIC_API_KEY` `docker/.env` orqali (bot tokeni naqshidek);
   kalit repoga yozilmaydi.

## 4. Qat'iy prinsiplar (boshidan, muhokama qilinmaydi)

1. **AI — foydalanuvchi sessiyasi ichida.** Alohida «super» hisob yo'q: tool
   bajarilish paytida o'sha RBAC'dan o'tadi (Telegram buyruqlaridagi naqsh),
   RLS tenant kontekstida ishlaydi. AI'ga berilgan ruxsat = foydalanuvchi ruxsati.
2. **Avval faqat O'QISH.** Birinchi bosqichlarda tool'lar hech narsa yozmaydi.
   Yozuvchi amallar (hujjat yaratish) — alohida bosqich va DOIM odam tasdiqlaydi
   (AI qoralama tayyorlaydi, foydalanuvchi «Tasdiqlash» bosadi).
3. **Javob — tool natijasidan.** Raqamli savolga model «eslab» javob bermaydi:
   tool chaqirilmagan bo'lsa, «aniqlab bera olmadim» deyiladi. System promptda
   qat'iy yoziladi.
4. **Ma'lumot ≠ ko'rsatma.** Tool natijalari (mahsulot nomlari, kontragent
   izohlari — foydalanuvchi kiritgan matn!) modelga «ma'lumot» deb uzatiladi;
   ulardagi «ko'rsatma»larga amal qilinmasligi system promptda belgilanadi
   (prompt injection himoyasi).
5. **Har chaqiriq auditda.** Kim, qachon, qaysi tool, qancha token — audit logga.
6. **Feature bilan yoqiladi.** `ai.chat` (keyin `ai.rag`, `ai.actions`) —
   plan/feature tizimi orqali; sinov davrida faqat demo/tanlangan tenantda.
7. **Muddat baholanmaydi** — faqat tartib (quyida).

## 5. Bosqichlar (tartib bo'yicha, muddatsiz)

### A0 — AI'dan oldingi dumlar (holat 2026-09-14 kechqurun)
- ✅ T2 (XATOLAR §9.3) — tuzatildi (`096f2be`), prod deploy qoldi.
- ✅ Platforma repo push — bajarildi (`8fea6fe`); WMS ham origin bilan teng.
- ⏳ XATOLAR §3 dagi ikki SQL so'rovi (prod, faqat o'qish) → FEFO/partiya qarori.
  To'liq ro'yxat va davomi — `WMS-AI-REJA.md` §P0.

### A1 — Telegram botda birinchi sinov (eng arzon yo'l)
**Nima:** buyruq bo'lmagan erkin matn `TelegramUpdateHandler` da AI'ga
yo'naltiriladi. AI 5–6 ta read-only tool bilan ishlaydi (mavjud so'rov
buyruqlarining servis-ekvivalentlari):
`stock_query` (qoldiq, ombor kesimida), `expiry_query` (muddati yaqin partiyalar),
`debt_query` (qarzdorlar/kreditorlar), `today_summary` (bugungi harakatlar),
`pending_transfers` (kutilayotgan hujjatlar), `find_product`/`find_counterparty`
(nom bo'yicha qidiruv — modelga ID topib beradi).

**Nega Telegram birinchi:** kanal, auth (deep-link ulash), tenant tanlash,
RBAC, xabar formati — hammasi TG1–TG17 da tayyor; migratsiya deyarli yo'q
(faqat suhbat jadvali); real foydalanuvchilarda tez sinov.

**Qabul mezoni:** «un qoldig'i qancha?» → to'g'ri jadval; «X firmaning qarzi?» →
to'g'ri summa; ruxsatsiz odam «ruxsat yo'q» oladi; AI o'ylab topgan raqam yo'q
(tool'siz javob bermaydi); har chaqiriq auditda.

### A2 — Web chat paneli
`/api/ai/chat` (SSE streaming), `ai_conversation`/`ai_message` jadvallari,
Angular yon panel (shell darajasida, har sahifadan ochiladi). Tool'lar A1 dagi
registrdan — ikki kanal bitta gateway'ga qaraydi. Kabinet (`/portal`) uchun ham
o'ylab qo'yiladi: mijoz «qarzim qancha?» deb so'raydi — tool'lar `client`/`agent`
rolining tor doirasida ishlaydi (PortalService orqali).

### A3 — RAG (hujjat bo'yicha yordam)
«Transfer qanday tasdiqlanadi?», «FEFO nima?» — javob qo'llanmadan.
Ishlar: pgvector image, `ai_document_chunk` jadvali, qo'llanma/docs'ni bo'laklab
embedding qilish (indeksatsiya — fon xizmat yoki seed-buyruq), `search_docs`
tool'i (AI kerak deb bilsa chaqiradi — alohida rejim emas, o'sha suhbat).
Kontent manbasi: `docs/qollanma/agentics-wms-qollanma.html` + `docs/*.md`.

### A4 — Yozuvchi amallar (tasdiqlash bilan)
«500 kg un chiqimini rasmiylashtir» → AI qoralama hujjat tayyorlaydi
(`draft_transfer` tool'i) → foydalanuvchiga ko'rsatiladi → «Tasdiqlash»
bosilgandagina mavjud servis (`TransferService.CreateAsync`) chaqiriladi.
AI hech qachon o'zi yakuniy yozuvni qilmaydi. Bu bosqichga A1–A2 ishonch
qozongandan keyingina o'tiladi.

### A5 — Boshqa mahsulotlarga ko'chirish (WMS'dan keyin)
Gateway naqshi (tool registry + suhbat + audit + limit) platforma darajasida
umumlashtiriladimi (masalan, `Agentics.Platform.*` paketi) — WMS tajribasidan
keyin hal qilinadi. Hozircha WMS ichida yoziladi, lekin WMS-domenga qattiq
bog'lanmagan qismlar (sikl, SSE, audit shakli) ajratiladigan qilib.

## 6. Texnik tanlovlar

- **Provayder/SDK:** Claude API, rasmiy Anthropic C# SDK (server tomonda,
  `WMS.Infrastructure` yoki `WMS.Ai` ichida). Tool-use sikli uchun SDK'ning
  tool runner'i yoki qo'lda sikl — gateway'da bitta joyda.
- **Model:** narxlar — `claude-opus-5` $5/$25, `claude-sonnet-5` $2/$10,
  `claude-haiku-4-5` $1/$5 (1M token, kirish/chiqish). REJA'dagi amaldagi
  tanlov — **`claude-sonnet-5`** (chat-tool routing uchun sifat/narx muvozanati);
  A5 da Haiku bilan, kerak bo'lsa Opus bilan eval to'plamida solishtiriladi.
  Tanlov konfigda (`Ai__Model`), kodga qotirilmaydi.
- **Thinking:** `thinking: { type: "adaptive" }` (hozirgi API'da `budget_tokens`
  eskirgan); oddiy so'rovlarda effort past bo'lishi mumkin.
- **Prompt caching:** system prompt + tool ta'riflari barqaror prefiks qilib
  yoziladi (o'zgaruvchan narsalar — sana, foydalanuvchi — oxirida), shunda har
  suhbat aylanishi arzonlashadi.
- **Tillar:** foydalanuvchi uz-Latn/ru yozadi — model ikkalasini ham tushunadi;
  javob tili foydalanuvchi profil tilidan (`TelegramLanguage` naqshidek).
- **Embedding (A3):** Anthropic embedding bermaydi — alohida provayder yoki
  ochiq model kerak bo'ladi; A3 boshlanishida tanlanadi (hozir qaror shart emas).
- **Xato shakli:** `/api/ai/*` ham `ApiResponse` + `code` qoidasiga bo'ysunadi
  (CLAUDE.md §3.6); AI ishlamay qolsa — aniq `ai_unavailable` kodi, tizimning
  qolgani ishlayveradi.

## 7. Xavflar va javoblar

| Xavf | Javob |
|---|---|
| AI raqam «o'ylab topadi» | Javob faqat tool natijasidan (§4.3); system promptda qat'iy; sinov savollari bilan tekshiriladi |
| Prompt injection (ma'lumot ichida ko'rsatma) | §4.4; tool'lar read-only; yozuvchi amallar faqat odam tasdig'i bilan |
| Tenant ma'lumoti sizishi | RLS + foydalanuvchi konteksti — AI alohida hisob emas; tool registry ruxsatga qarab qisqaradi |
| Xarajat nazoratsiz o'sadi | Tenant/kun limiti, `usage` logi, model konfigda; `ai.chat` feature'ini o'chirish — bir tugma |
| Bitta so'rov sekin (LLM ~soniyalar) | Telegram'da «yozmoqda…» harakati; webda SSE streaming; tool'lar yengil so'rovlar |
| API kaliti sizishi | `.env` orqali, repoda yo'q; prod'da faqat serverda |

## 8. Birinchi amaliy qadam (A1 boshlanishi)

1. `A0` dumlarini yopish.
2. Anthropic SDK + `AddAiModule()` skeleti, `Ai__ApiKey`/`Ai__Model` konfigi.
3. Tool registry (6 read-only tool) — Telegram so'rov buyruqlari servislaridan.
4. `TelegramUpdateHandler`: buyruq emas + `ai.chat` yoqiq → AI gateway.
5. Suhbat jadvali + audit + tenant limiti.
6. Demo tenantda sinov savollari ro'yxati bilan qabul testi.

---

*REJA yozildi — `WMS-AI-REJA.md` (F10). Bu yerdagi ochiq savollar o'sha yerda
javob oldi: A1 — avval Telegram, A2 — web; `ai.chat` — demo tenant; kunlik
chegara — `Ai__DailyUsdCap` (sukut $10). Farqlar: REJA RAG'ni pgvector'siz
boshlaydi (`pg_trgm` qidiruv, K bo'limi) va asosiy model sifatida
`claude-sonnet-5` ni tanladi — REJA ustun.*
