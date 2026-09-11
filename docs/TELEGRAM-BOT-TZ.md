# Telegram bot — texnik topshiriq (TG1–TG17)

> **Terminal:** backend (`src/`) + frontend (`frontend/apps/wms-web`) · **Branch:** `telegram-bot` (F7 deploy'dan keyin, `main` dan)
> **Asos:** 2026-09-11 kod tekshiruvi. Bosqich 3 dagi «Telegram bildirishnomalar» (`34fb40b`,
> 2026-07-02) F6 da PostgreSQL/Identity'ga ko'chirilgan holda ishlaydi, lekin **mijoz uni
> ishlata olmaydi** (TG1 dagi sabab).
>
> Hujjat ikki qism: **I — kanalni tuzatish** (TG1–TG8: ulash, navbat, kim nimani oladi,
> shakl, obuna ogohlantirishlari) va **II — bot ish quroli sifatida** (TG9–TG17: tugma
> bilan tasdiqlash, so'rovlar, kunlik xulosa, haydovchi, mijoz, davomat, hisobot, guruh,
> platforma egasi kanali). II qism I qismsiz boshlanmaydi.

---

## 0. Kontekst

### Hozir nima bor

| Qism | Qayerda |
|---|---|
| Bot API orqali yuborish; token bo'lmasa no-op; token logga tushmaydi | `Services/Operations/TelegramService.cs`, `OperationsModule.cs` (nomlangan `HttpClient`, 10 s timeout, loggerlar o'chirilgan) |
| Har yangi bildirishnoma Telegram'ga ham ketadi (so'rov ichida, ketma-ket) | `NotificationService.CreateAsync` → `ForwardToTelegram` |
| `user_profile.telegram_chat_id` (varchar 64 — TG1 da `telegram_link` jadvaliga ko'chadi), `GET/PUT /api/me/telegram` | `MeTelegramController.cs`, `AccessConfiguration.cs` |
| Profil sahifasida «Chat ID» maydoni + «bot sozlanmagan» ogohlantirishi | `features/settings/profile/*` |

Bildirishnoma manbalari (hammasi `CreateAsync(null, …)` — tenantdagi **hammaga**):

| Manba | `NotificationType` |
|---|---|
| `TransferService` — tasdiq, rad, qaytarish, kam zaxira | `TransferConfirmed`, `TransferRejected`, `Info`, `LowStock` |
| `ProductionService` — boshlandi, tugadi | `ProductionStarted`, `ProductionCompleted` |
| `BatchExpiryService` (kunlik fon, har tenant o'z scope'ida) | `BatchExpiring`, `BatchExpired` |

### Nima yo'q / noto'g'ri

| # | Ish | Nega |
|---|---|---|
| **TG1** | Deep-link orqali ulash + `/start` qabul qilish | Bot foydalanuvchiga u `/start` bosmaguncha yoza olmaydi (403). Hozirgi «chat ID ni @userinfobot dan oling» yo'li amalda ishlamaydi va xato faqat logda |
| **TG2** | Outbox + fon yuboruvchi | Yuborish so'rov ichida: N ta ulangan foydalanuvchi × 10 s — transferni tasdiqlash osilib qoladi. Telegram limitlari (429) va bloklangan bot (403) qayta ishlanmaydi |
| **TG3** | Kim qaysi xabarni oladi | Hamma xabar hammaga: kuzatuvchi ham ishlab chiqarish xabarini oladi. O'chirib qo'yish imkoni yo'q |
| **TG4** | Xabar shakli | Tenant nomi yo'q (bir odam ikki zavodda bo'lsa ajrata olmaydi), tilsiz, havolasiz |
| **TG5** | R6/R7 — obuna va limit ogohlantirishlari | Trial tugashi haqida hech kim xabar olmaydi; fon xizmati faqat to'xtatadi |
| **TG6** | Bot buyruqlari `/stop`, `/status`, `/help` | Ulanganini uzish yoki tekshirish faqat web'da |
| **TG7** | Frontend: ulash oqimi | Chat ID maydoni o'rniga tugma + holat + o'chirgichlar |
| **TG8** | Config, deploy, hujjat | Hech bir `.env`/compose'da token yo'q — prod'da bot o'chiq |

**Tartib (I qism):** TG8 (config) → TG1 → TG2 → TG7 → TG3 → TG4 → TG5 → TG6.
TG1 va TG2 bitta PR bo'lishi mumkin; TG7 TG1 ni kutadi. II qism (TG9–TG17) tartibi —
o'z bo'limida.

### Qarorlar (muhokama qilinmaydi)

- **Polling, webhook emas** (v1). Sabab: bitta API instansi; ochiq endpoint, secret va
  dev'da tunnel kerak emas. `getUpdates` va webhook bir vaqtda ishlamaydi — startupda
  `deleteWebhook` chaqiriladi. Webhook — keyin, instans ko'paysa.
- **Faqat shaxsiy chat** (`chat.type == "private"`). Guruh xabarlari e'tiborsiz qoldiriladi.
- **Ulanish alohida jadvalda — `wms.telegram_link`** (Wash naqshi), `user_profile.telegram_chat_id`
  ustuni **o'chadi** (prod ma'lumoti yo'q, demo seed). Sabab: TG12/TG13 da haydovchi va
  kontragent ham ulanadi — «bu chat kimniki» savoliga uch jadvaldan emas, bittadan javob
  bo'lsin; bloklaganda yozuv o'chirilmaydi (`is_active = false`), qaytib kelsa tarix bor.
  Bir Telegram akkaunt bir necha tenantda ulangan bo'lishi mumkin — bu me'yor.
- **Token va outbox — platforma jadvali** (`Tenant` kabi `BaseEntity`, RLS yo'q): ular tenant
  kontekstisiz o'qiladi (polling, yuboruvchi). `tenant_id` ularda oddiy ustun.
  `telegram_link` esa tenant jadvali (RLS) — unga har doim scope bilan kiriladi.
- Qo'lda chat ID kiritish **olib tashlanadi** (`PUT /api/me/telegram`). Deep-link bo'lgach
  unga hojat yo'q, va u ishlamaydigan yo'l edi.

### Wash bilan moslik (`agentics-wash`, F3 — `docs/PROD-RUNBOOK.md` §4.2, `Modules/Messaging`)

Wash'da Telegram to'liq ishlaydi va prod'da `AgenticsWashBot` bor. Kod umumiy paketda
emas (`Wash.Application`/`Wash.Domain`), shuning uchun **naqsh ko'chiriladi, kod emas**.
Nima olinadi, nima ataylab boshqacha:

| Wash | WMS | Nega |
|---|---|---|
| `telegram_link` (tenant, RLS): `customer_id?`/`user_id?`, `chat_id` (long), `username`, `lang`, `linked_via`, `is_active` | Xuddi shu, `driver_id?`/`counterparty_id?` bilan | Bitta manba |
| `message_outbox`: `pending/sent/failed/skipped`, `dedup_key` (unique), 5 urinish, 1/2/4/8 daqiqa | Xuddi shu holatlar, `dedup_key`, backoff | TG5 dedupe shu kalit bilan — bildirishnoma jadvalini qidirmasdan |
| Bloklash `my_chat_member` update'idan (`Blocked`) + yuborishda 403 | Ikkalasi | 403 ni kutmasdan darhol `is_active = false` |
| Til — Telegram `language_code` (`uz`/`ru`), yo'q bo'lsa tenant sukuti | Xuddi shu (`telegram_link.lang`) | TG4 dagi «profilga til ustuni» savoli yopiladi |
| Bot javoblari kodda, shablon jadvalida emas; darhol, outbox'siz | Xuddi shu | Interfeys matni |
| Webhook, tenant kodi manzilda, sir sarlavhasi fail-closed | **Polling** (v1) | Wash'da har moyka O'Z boti — ko'p botni polling qilib bo'lmaydi. WMS'da bitta umumiy bot; `ITelegramUpdateHandler` manbadan mustaqil, webhook keyin qo'shiladi |
| Tenantning o'z boti (token UI'da, `ISecretProtector` + `Secrets:MasterKey`) | v1 da **yo'q** | Xodimga «Agentics WMS» botidan xabar kelishi me'yor. TG13 (mijoz boti) da Wash'ning dalili kuchga kiradi — o'sha vaqtda `ISecretProtector` va webhook bilan birga |
| Deep-link payload'ida `tenant_customer` (Guid) | Tasodifiy bir martalik token, 10 daqiqa | Guid v7 taxmin qilinmaydi, lekin muddatsiz; token qat'iyroq va arzon |
| Hangfire job, har tenant scope'da | `BackgroundService` | WMS'da Hangfire yo'q (D12) |
| `TELEGRAM_BOT_USERNAME` env | `getMe` dan | Wash'da tenant botlari katalogi bor, bizda bitta bot |

Ko'chirib olsa bo'ladigan fayllar (o'qib, WMS uslubiga moslab): `Wash.Domain/Messaging/TelegramUpdate.cs`
(update o'qish: `Start`/`Contact`/`Blocked`, begona kontakt), `Wash.Infrastructure/Messaging/TelegramBotApi.cs`
(HTTP qatlam, token URL'da — log'ga tushmasligi), `TelegramBotReplies.cs` (javob matnlari tuzilishi).

### Qoidalar

- `dotnet build AgenticsWms.slnx` → **0 xato, 0 ogohlantirish**. API ishlayotgan bo'lsa
  `bin/` band — avval to'xtating.
- Javob `ApiResponse<T>`, xabar — inglizcha shablon = tarjima kaliti (`Translations.cs`).
- Tenant parametr sifatida uzatilmaydi; fon ishida — `ICurrentTenant.Set` yoki `TenantScopes`.
- **Bot tokeni hech qayerda logga tushmasin** — `TelegramService` dagi ikki izoh
  (`RemoveAllLoggers`, istisno obyekti logga berilmaydi) saqlanadi. Yangi HTTP chaqiruvlar
  ham o'sha nomlangan client orqali.
- Yangi migratsiya: `dotnet ef migrations add <Nom> -p src/WMS.Infrastructure -s src/WMS.Infrastructure -o Persistence/Migrations`.
- Izohlar o'zbekcha, «nega» ni aytadi.

---

# 🔴 TG8 — Config va deploy (birinchi, 30 daqiqa)

## Ishlar

`appsettings.json` ga bo'lim (sukutlar bilan), `TelegramOptions` (`SectionName = "Telegram"`,
`SubscriptionOptions` naqshi):

```json
"Telegram": {
  "BotToken": "",
  "PollingIntervalSeconds": 2,
  "LinkTokenMinutes": 10,
  "OutboxBatchSize": 50,
  "OutboxRetentionDays": 14
}
```

- `docker/.env.example`: `TELEGRAM_BOT_TOKEN=` (bo'sh — bot o'chiq; izoh: `@BotFather` dan,
  prod'da parol menejerida). `docker-compose.prod.yml` va dev compose: `Telegram__BotToken: ${TELEGRAM_BOT_TOKEN:-}`.
- Bot username **konfiguratsiyada emas** — startupda `getMe` bilan bir marta olinadi va
  keshlanadi (`ITelegramService.BotUsername`). Sabab: ikki manba ajralib ketadi.
- `TelegramService` `IsEnabled` mantiqi o'zgarmaydi: token bo'sh — hamma narsa no-op,
  polling va yuboruvchi fon xizmatlari ham ishga tushmaydi (loglarda bitta qator).
- `docs/CLAUDE.md` §4 ga bir qator: bot uchun `TELEGRAM_BOT_TOKEN`, dev'da o'zingizning
  test botingiz (prod bilan bir xil token ishlatilmaydi — polling talashadi).

## Qabul mezoni

- Token bo'sh: API ko'tariladi, logda bitta «sozlanmagan» qatori, fon xizmatlari ishlamaydi.
- Token noto'g'ri: `getMe` 401 → logda ogohlantirish (tokensiz), bot o'chiq deb qabul qilinadi, API yiqilmaydi.

---

# 🔴 TG1 — Deep-link orqali ulash va `/start`

## Muammo

Telegram bot foydalanuvchi unga birinchi bo'lib yozmaguncha (`/start`) xabar yubora
olmaydi — `sendMessage` 403 qaytaradi. Hozirgi oqimda foydalanuvchi chat ID ni saqlaydi,
xabar kelmaydi, sabab logda qoladi. To'g'ri oqim: web → bir martalik token bilan havola →
foydalanuvchi botda `/start <token>` → bot o'zi `chat_id` ni profilga yozadi.

## Ishlar

### Jadval `wms.telegram_link_token` (platforma, RLS yo'q)

| Ustun | Izoh |
|---|---|
| `id` | Guid v7 |
| `token` | 32 belgi, `[A-Za-z0-9_-]` (base64url, 24 tasodifiy bayt), **unique** |
| `tenant_id`, `user_profile_id` | Kim uchun |
| `expires_at` | `now + LinkTokenMinutes` |
| `used_at` | Ishlatilgach to'ldiriladi; bir martalik |

Sabab: `/start` kelganda tenant konteksti yo'q — token platforma jadvalida bo'lmasa
uni topib bo'lmaydi (RLS 0 qator beradi). Topilgach `ICurrentTenant.Set(tenantId, code)`
qilib `telegram_link` yoziladi (`TenantScopes` dagi naqsh, bitta scope).

Deep-link payload chegarasi: 64 belgi, `[A-Za-z0-9_-]` — 32 belgi yetarli.

### Jadval `wms.telegram_link` (tenant, RLS — `TenantEntity`)

| Ustun | Izoh |
|---|---|
| `user_profile_id` | Xodim (TG12/TG13 da `driver_id?`, `counterparty_id?` qo'shiladi; aynan bittasi to'la — CHECK) |
| `chat_id` | `bigint` (Telegram int64; eski ustun `varchar(64)` edi) |
| `username`, `first_name` | `/status` va audit uchun |
| `lang` | Telegram `language_code` dan (`uz`/`ru`; boshqasi → `uz`) |
| `linked_via` | `deep_link` (keyin `contact_share` — TG12) |
| `is_active` | Bloklaganda `false`; yozuv o'chirilmaydi |
| `muted_types`, `digest` | TG3, TG11 sozlamalari — Telegram'ga xos, profilga emas |

Unique: `(tenant_id, user_profile_id)`. Bir profil — bitta chat; boshqa akkauntdan qayta
ulansa yozuv **almashadi** (Wash `Reconnect`), ikkinchi qator yaratilmaydi — aks holda
xabar ikki chatga ketardi. `user_profile.telegram_chat_id` ustuni migratsiyada o'chadi;
`AccessConfiguration` va `WmsPlatformUserSink` dagi izohlar yangilanadi.

### API (`MeTelegramController`, `/api/me/telegram`)

| So'rov | Javob |
|---|---|
| `GET` | `{ enabled, botUsername, linked, linkedAt, mutedTypes[] }` (`mutedTypes` — TG3) |
| `POST link-token` | `{ url: "https://t.me/<bot>?start=<token>", expiresAt }`. Foydalanuvchining eskirmagan tokenlari **bekor qilinadi** (bitta faol token) |
| `DELETE` | Ulanishni uzadi (`is_active = false`; qayta ulash — yangi token, yozuv almashadi) |
| ~~`PUT`~~ | O'chiriladi (`SetTelegramDto` ham) |

Bot o'chiq bo'lsa (`enabled = false`) `POST link-token` → 400, `code: "telegram_disabled"`.

### Polling fon xizmati `TelegramPollingBackgroundService` (Operations moduli)

- Faqat `IsEnabled` bo'lsa ishlaydi. Startupda `deleteWebhook`, keyin `setMyCommands`
  (`start`, `stop`, `status`, `help` — TG6).
- `getUpdates?timeout=30&offset=<last+1>&allowed_updates=["message","my_chat_member"]`.
  Xato bo'lsa `PollingIntervalSeconds` kutib qayta uriniladi; xizmat **hech qachon o'lmaydi**
  (`SubscriptionExpiryBackgroundService` dagi `catch` naqshi).
- Update o'qish — Wash `TelegramUpdate.cs` naqshi: `Start(payload)`, `Command`, `Blocked`
  (`my_chat_member` → `kicked`), `Unblocked`, `Other`. `ITelegramUpdateHandler` polling'dan
  mustaqil — keyin webhook shu interfeysga keladi.
- Har update alohida scope'da, alohida `try` — bitta buzuq xabar navbatni to'xtatmasin.
- `/start <token>`: token topilmasa/eskirgan/ishlatilgan → «Havola eskirgan, WMS profilidan
  yangisini oling». Topilsa: `used_at`, `telegram_link` yoziladi/almashadi (`chat_id`,
  `username`, `lang = language_code`), javob (`lang` da): «✅ *<Tenant nomi>* ga ulandi.
  Bildirishnomalar shu yerga keladi.»
- `Blocked`: chat bo'yicha barcha tenantlarda `is_active = false` (`TenantScopes`; kam
  hodisa). `Unblocked` — hech narsa: foydalanuvchi qayta `/start <token>` qiladi.
- `/start` (payloadsiz) va notanish matn → qisqa yo'riqnoma: «Ulash uchun WMS → Sozlamalar →
  Profil → Telegram'ga ulash» (TG6 da `/help` bilan birlashadi).
- `chat.type != "private"` → e'tiborsiz.

### `NotificationService`

- `GetTelegramChatAsync` / `SetTelegramChatAsync` o'rniga `GetTelegramStatusAsync`,
  `CreateLinkTokenAsync`, `UnlinkTelegramAsync`. Interfeys izohini yangilang.
- `ITelegramService` ga: `BotUsername`, `GetUpdatesAsync`, `DeleteWebhookAsync`,
  `SetMyCommandsAsync`. Javob DTO'lari minimal (faqat kerakli maydonlar), `System.Text.Json`.

## Qabul mezoni

- Profil sahifasidan olingan havola bosilganda bot «ulandi» deb javob beradi va
  `GET /api/me/telegram` → `linked = true`.
- Havola ikkinchi marta bosilsa → «eskirgan». 10 daqiqadan keyin ham → «eskirgan».
- Ikki tenantda profili bor odam ikkala tenantdan ulanadi — ikkita `telegram_link`, bir xil `chat_id`.
- `DELETE` dan keyin xabar kelmaydi; qayta ulash ishlaydi va ikkinchi qator yaratmaydi.
- Botni bloklagan foydalanuvchi 1 daqiqa ichida `is_active = false` (yuborishni kutmasdan).
- Ruscha Telegram'li foydalanuvchi «ulandi» javobini ruscha oladi.
- Guruhga qo'shilgan bot hech narsa yozmaydi.
- Token noto'g'ri bo'lganda logda token ko'rinmaydi (loglarni ko'zdan kechiring).

---

# 🔴 TG2 — Outbox va fon yuboruvchi

## Muammo

`CreateAsync` Telegram'ga yuborishni **kutadi**: ulangan har foydalanuvchi uchun ketma-ket
HTTP, har biri 10 s gacha. Transfer tasdig'i, ishlab chiqarish bosqichi — hammasi shu
yo'ldan o'tadi. Telegram 429 (`retry_after`) qaytarsa xabar yo'qoladi; 403 (foydalanuvchi
botni bloklagan) har safar qayta uriniladi.

## Ishlar

### Jadval `wms.telegram_outbox` (platforma, RLS yo'q)

| Ustun | Izoh |
|---|---|
| `id` | Guid v7 |
| `tenant_id`, `telegram_link_id` | 403 da ulanishni topish uchun (scope shu bilan ochiladi); TG17 ops xabarida ikkalasi `null` |
| `chat_id` | Yuborish paytidagi qiymat (`bigint`) |
| `text` | Tayyor HTML (TG4), ≤ 4096 belgi — uzunini kesib `…` |
| `notification_id` | Izlash uchun (FK **emas** — bildirishnoma tenant jadvalida) |
| `dedup_key` | Unique (null'lar bundan mustasno): bildirishnoma uchun `"n:{notificationId}:{chatId}"` — hodisa darajasida (`type:entityId`) EMAS: kam zaxira har transferda qonuniy qayta chiqadi, bunday kalit keyingi ogohlantirishlarni yutardi (hodisa dedupe'i — bildirishnoma yaratuvchisiniki, `BatchExpiryService` kabi). TG5/TG11 o'z kalitlari bilan (`"trial:{tenantId}:{sana}:{chatId}"`, `"digest:{sana}:{chatId}"`) |
| `status` | `pending`, `sent`, `failed`, `skipped` (`skipped` — ulanish uzilgan/bloklangan: xato emas, jurnalni to'ldirmasin) |
| `attempts`, `next_attempt_at`, `last_error` | Qayta urinish; `last_error` — faqat HTTP kod va Telegram `description`, URL emas |
| `created_at`, `sent_at` | |

Indeks: `(status, next_attempt_at)`; unique `dedup_key`.

### Yozish

`NotificationService.CreateAsync` **so'rov ichida** (tenant konteksti bor) qabul qiluvchilarni
aniqlaydi (TG3 gacha — `telegram_link.is_active && user_profile.is_active`), matnni
tayyorlaydi va outbox qatorlarini bildirishnoma bilan **bitta `SaveChanges`** da yozadi.
HTTP chaqiruv so'rovda **bo'lmaydi**. `ForwardToTelegram` o'chadi.

### `TelegramOutboxBackgroundService`

- Har `PollingIntervalSeconds` da `Pending && next_attempt_at <= now` dan `OutboxBatchSize`
  ta (eng eskisi birinchi), bitta scope, `SaveChanges` har qatordan keyin (yarim yuborilgan
  partiya restartda takrorlanmasin).
- Tezlik: umumiy ~25 xabar/s, bir chatga ~1 xabar/s (Telegram: 30/s va 1/s). Oddiy
  `SemaphoreSlim` + chat bo'yicha oxirgi yuborish vaqti yetarli; kutubxona kerak emas.
- Javoblar:

| Javob | Amal |
|---|---|
| 200 | `sent`, `sent_at` |
| 429 | `next_attempt_at = now + retry_after` (bo'lmasa 5 s), `attempts` oshmaydi |
| 403 (`bot was blocked`), 400 (`chat not found`) | `skipped`; `telegram_link.is_active = false` (scope `tenant_id` bilan). Logda `Information` |
| 5xx, timeout, tarmoq | `attempts++`, `next_attempt_at = now + 1/2/4/8 daqiqa` (Wash `BackoffFor`); 5 urinishdan keyin `failed` |

- Yuborish oldidan ulanish `is_active` qayta tekshiriladi — o'rtada uzilgan bo'lsa `skipped`.
- `sent`/`failed`/`skipped` qatorlar `OutboxRetentionDays` dan keyin o'chiriladi (shu xizmatda, kuniga bir).
- `TelegramService.SendMessageAsync` natija qaytaradi (`TelegramSendResult { Ok, StatusCode, RetryAfter, Description }`) — `void` emas.

### `BatchExpiryService` bilan bog'liq

Fon ishi `TenantScopes` ichida `CreateAsync` ni chaqiradi — o'sha scope'da tenant konteksti
bor, outbox yozuvi shunchaki qatorga aylanadi. O'zgarish talab qilmaydi; tekshiring.

## Qabul mezoni

- 20 ta ulangan foydalanuvchi bo'lgan tenantda transfer tasdig'i **< 1 s** javob beradi
  (yuborish so'rovdan tashqarida).
- API to'xtatib qayta ko'tarilganda `Pending` qatorlar yuboriladi, yuborilganlar takrorlanmaydi.
- Botni bloklagan foydalanuvchi: bitta 403 → ulanish `is_active = false`, keyingi bildirishnomada unga qator yozilmaydi.
- Bitta hodisa ikki marta ishlansa (masalan `BatchExpiryService` ikki marta yugursa) ikkinchi outbox qatori yozilmaydi.
- 429 sun'iy tekshiruv (masalan 100 ta bildirishnomani ketma-ket yaratish): xabar yo'qolmaydi, kechikib yetadi.
- `last_error` da token yoki URL yo'q.

---

# 🟠 TG7 — Frontend: ulash oqimi (profil sahifasi)

## Ishlar

`features/settings/profile/*` dagi Telegram bo'limi:

| Holat | Ko'rinish |
|---|---|
| `enabled = false` | Hozirgi ogohlantirish qoladi («bot hali sozlanmagan»), tugma yo'q |
| `linked = false` | Matn: «Bildirishnomalarni Telegram'da olish uchun ulang» + tugma **«Telegram'ga ulash»**. Bosilganda `POST link-token` → `url` yangi oynada (`window.open`, `noopener`). Keyin har 3 s `GET` (eng ko'pi `expiresAt` gacha) — `linked` bo'lgach to'xtaydi va muvaffaqiyat toast. Eskirsa: «Havola eskirdi» + qayta tugma |
| `linked = true` | «✅ Ulangan (@bot, <sana>)» + tugma **«Uzish»** (`DELETE`, tasdiq dialogisiz — qayta ulash arzon). Pastda TG3 o'chirgichlari |

- Chat ID maydoni, `placeholder="123456789"`, `@userinfobot` matni — **o'chiriladi**.
- `settings.model.ts`: `TelegramLink` → `TelegramStatus { enabled, botUsername, linked, linkedAt, mutedTypes }`, `TelegramLinkToken { url, expiresAt }`.
- `settings.service.ts`: `getTelegram()`, `createTelegramLink()`, `unlinkTelegram()`, `setTelegramMuted(types)` (TG3).
- i18n (`uz-Latn.json`, `ru.json`): `telegramHint`, `telegramChatId` o'rniga `telegramLink`, `telegramLinked`, `telegramUnlink`, `telegramLinkExpired`, `telegramLinkWaiting`. Ikkala tilda ham.
- `npx nx run-many -t lint,test,build -p wms-web` toza.

## Qabul mezoni

- Tugma → Telegram ochiladi → `/start` → 3 s ichida sahifa «Ulangan» ga o'tadi (qo'lda yangilamasdan).
- Oynani yopib qo'ysa polling `expiresAt` da to'xtaydi (tarmoq panelida cheksiz so'rov yo'q).
- 400 px enda buzilmaydi.

---

# 🟠 TG3 — Kim qaysi xabarni oladi

## Muammo

Barcha bildirishnomalar `UserId == null` (hammaga). Telegram'da bu «kuzatuvchi» roli
ham ishlab chiqarish bosqichi tugaganini oladi degani. Xabar ko'p bo'lsa foydalanuvchi
botni bloklaydi — va TG2 uni uzadi. O'chirgich yo'q.

## Ishlar

### Tur → ruxsat xaritasi (kodda, `WmsPermissions` yonida)

`NotificationRouting.RequiredPermission(NotificationType)`:

| Tur | Ruxsat |
|---|---|
| `LowStock`, `BatchExpiring`, `BatchExpired` | `warehouse.view` |
| `TransferConfirmed`, `TransferRejected`, `Info` (qaytarish) | `transfers.view` |
| `ProductionStarted`, `ProductionCompleted` | `production.view` |
| `Warning`, `Error` | hamma (`null`) |

Broadcast bildirishnoma (`UserId == null`) uchun Telegram qabul qiluvchilari:
`telegram_link.is_active && user_profile.is_active` **va** rollari orqali shu ruxsatga ega
(`UserRole → Role → RolePermission.PermissionCode`). Bitta so'rov, `Distinct`.
Ilova ichidagi bildirishnoma (`GET /api/notifications`) **o'zgarmaydi** — bu faqat
Telegram kanali uchun filtr; ilova ichida hamma ko'raverishi hozircha qaror (HOLAT: «umumiy
bildirishnomaning bitta o'qildi belgisi» — alohida masala).

`Info` turi qaytarish uchun ishlatilgan — `TransferService` da uni `ReturnReceived` (yangi
enum qiymati 11) ga o'tkazing; `Info` umumiy tur bo'lib qoladi.

### O'chirgichlar

`telegram_link.muted_types` — `text` (vergul bilan ajratilgan enum nomlari;
`null` — hech narsa o'chirilmagan). Bitmask emas: enum kengayganda o'qish oson.
`PUT /api/me/telegram/muted { types: string[] }` — nomlar `NotificationType` da bo'lishi
shart, aks holda 400. Frontend (TG7): guruhlangan checkbox'lar — «Zaxira va partiyalar»,
«Transferlar», «Ishlab chiqarish». Faqat ruxsati bor guruhlar ko'rsatiladi.

Qabul qiluvchi hisoblashda `mutedTypes.Contains(type)` bo'lganlar tushib qoladi.

## Qabul mezoni

- `viewer` roli faqat `.view` ruxsatlarini oladi — u hamma xabarni **oladi** (view yetarli), lekin
  `warehouse.view` olib tashlangan maxsus rol `LowStock` olmaydi.
- «Ishlab chiqarish» o'chirilgan foydalanuvchi `ProductionCompleted` olmaydi, ilovada esa ko'radi.
- Noma'lum tur nomi `PUT muted` da 400.

---

# 🟠 TG4 — Xabar shakli

## Muammo

Hozir: `<b>Title</b>\nMessage`. Sarlavha inglizcha (`"Transfer Confirmed"`), matn aralash
(`"Transfer #<guid> has been confirmed"` / `"partiyasi muddati o'tdi"`), tenant nomi yo'q,
ilovaga havola yo'q.

## Ishlar

Format (HTML, `WebUtility.HtmlEncode` saqlanadi):

```
🏭 <b>{Tenant.Name}</b>
{emoji} <b>{Sarlavha}</b>
{Matn}

<a href="{PUBLIC_WEB_URL}/{yo'l}">Ochish</a>
```

- Emoji tur bo'yicha: ⚠️ `LowStock`/`Batch*`, ✅ `TransferConfirmed`/`ProductionCompleted`,
  ❌ `TransferRejected`, ▶️ `ProductionStarted`, ↩️ `ReturnReceived`.
- **Til:** sarlavha va matn `Translations.cs` orqali. Bildirishnoma manbalari inglizcha
  shablon yozadi (`"Transfer Confirmed"`, `"{0} stock is low ({1} {2} remaining). Min: {3}"`) —
  shu shablonlar **kalit**. Hozir `TransferService` va `ProductionService` matnni
  interpolatsiya bilan yozadi — ularni `Messages` konstantasi + `string.Format` ga o'tkazing,
  `BatchExpiryService` dagi o'zbekcha matnni ham. Til: `telegram_link.lang` (Telegram
  `language_code` dan, TG1); `uz`/`ru` dan boshqasi → `Telegram:DefaultLanguage = "uz"`.
  Profilga til ustuni **qo'shilmaydi**. Ilova ichidagi bildirishnoma matni o'zgarmaydi —
  frontend uni qanday ko'rsatayotgan bo'lsa shunday qoladi.
- **Havola:** `EntityType`/`EntityId` bo'yicha: `Transfer` → `/transfers/{id}`, `Product` →
  `/products/{id}`, `Batch` → `/warehouse/batches?batch={id}`, `ProductionOrder` →
  `/production/orders/{id}`. wms-web marshrutlarini `app.routes.ts` dan tekshiring — yo'q
  bo'lsa havola qo'yilmaydi (buzuq havola yo'q havoladan yomon). Baza URL —
  `Cors:AllowedOrigins[0]` emas, alohida `Telegram:WebUrl` (prod `https://wms.agentics.uz`).
- Guid ko'rsatilmaydi: `Transfer #{id}` o'rniga sana + kontragent (`"Kirim, Ice Gold, 11.09"`);
  qisqa raqam qarori (HOLAT «keyinga qolgan») kelganda shu joy o'zgaradi — bitta joyda (`NotificationText`).
- Matn 4096 dan uzun bo'lsa kesiladi (TG2).

## Qabul mezoni

- Ikki tenantga ulangan odam ikkala xabarda tenant nomini ko'radi.
- Mahsulot nomida `<` yoki `&` bo'lsa xabar yetib boradi.
- Havola ilovadagi to'g'ri sahifani ochadi (login'dan keyin ham — `returnUrl` ishlashini tekshiring).
- `Translations.cs` da dublikat kalit yo'q (ilova ko'tariladi).

---

# 🟡 TG5 — R6/R7: obuna va limit ogohlantirishlari

## Muammo

Trial tugayotganini mijoz faqat kirganda banner'da ko'radi. Fon xizmati
(`SubscriptionExpiryBackgroundService`) faqat to'xtatadi. Limitning 80 % iga yetganda
`ApiResponse.Warning` beriladi (`LimitWarn*`), lekin bildirishnoma yaratilmaydi.

## Ishlar

### R6 — muddat

`SubscriptionExpiryBackgroundService.RunAsync` ga 4-qadam: `TrialEndsAt` yoki `PaidUntil`
`now + WarnBeforeDays` oralig'ida bo'lgan tenantlar. Har biri uchun `ICurrentTenant.Set`
bilan scope (xizmat ataylab `TenantScopes` ishlatmaydi — izohini o'qing; bu qadam uchun
faqat o'sha tenantlarga scope ochiladi) va `CreateAsync(null, …, NotificationType.SubscriptionWarning)`
(yangi qiymat 12), `EntityType = "Tenant"`, `EntityId = tenant.Id`.

- **Dedupe:** ilova ichidagi bildirishnoma uchun — `EntityType = "Tenant"` +
  `CreatedAt > PaidUntil - WarnBeforeDays` tekshiruvi; Telegram uchun — outbox `dedup_key`
  `"trial:{tenantId}:{sana:yyyyMMdd}:{chatId}"`. Sana o'zgarsa (to'lov uzaytirildi) — yangi kalit, yangi sikl.
- **Qabul qiluvchi (TG3):** faqat `settings.modules` ruxsati borlar (amalda `admin`).
- Matn: «Sinov muddati {0} da tugaydi. Davom etish uchun tarif tanlang» / «To'lov muddati {0} da tugaydi».
- Tenant suspend bo'lganda ham bitta xabar (`SubscriptionSuspended`, 13): «Hisob to'xtatildi: to'lov».

### R7 — limit

`LimitWarn*` ogohlantirishi hosil bo'ladigan joylar (`PlanLimits`, `WarehouseService`,
`TransferService`, `SubscriptionService`) — o'sha joyda `CreateAsync(null, …, NotificationType.LimitWarning)`
(14), `EntityType = "Plan"`. Dedupe: bir tenantda bir limit turi uchun **oyiga bitta**
(transfer limiti oylik; foydalanuvchi/ombor limiti — qiymat o'zgargunga qadar bitta:
`EntityId` o'rniga `Message` shabloni + `CreatedAt` bo'yicha tekshirish).
Qabul qiluvchi — `settings.modules` ruxsati borlar.

⚠️ `PlanLimits` izohi: hisob so'rovdan tashqarida, ogohlantirish amaliyotni yiqitmasin —
bildirishnoma yaratish ham `NotifySafelyAsync` naqshida.

## Qabul mezoni

- Trial 7 kun qolganda tenant admini bitta Telegram xabar oladi; ertasi kuni ikkinchisi kelmaydi.
- To'lov 1 oyga uzaytirilsa, keyingi 7-kun oynasida yana xabar keladi.
- Transfer limiti 80 % ga yetganda oyda bitta xabar.
- Bildirishnoma yaratish yiqilsa transfer/ombor amaliyoti baribir muvaffaqiyatli.

---

# 🟡 TG6 — Bot buyruqlari

## Ishlar

Polling xizmati (TG1) ichida:

| Buyruq | Amal |
|---|---|
| `/start <token>` | TG1 |
| `/start`, `/help` | Yo'riqnoma + tenantlar ro'yxati (ulangan bo'lsa) |
| `/status` | Chat ulangan barcha profillar: «🏭 Ice Gold — admin, 3 tur o'chirilgan». Topish: `TenantScopes.ForEachTenantAsync` bilan har tenantda `telegram_link.chat_id == chat.id && is_active` (kam chaqiriladigan buyruq — N ta scope maqbul; TG1 dagi `Blocked` ham shu yo'l) |
| `/stop` | Barcha ulanishlarni `is_active = false` qiladi (`/status` bilan bir xil izlash), javob: «Uzildi. Qayta ulash — WMS profilidan» |
| boshqa | `/help` matni |

- Buyruq javoblari `Telegram:DefaultLanguage` da, `Translations` orqali.
- `setMyCommands` — startupda (TG1), ikki tilda (`language_code` `uz`, `ru`).
- Bot faol emas tenantning (`IsActive = false`) profilini `/status` ko'rsatmaydi (`ForEachTenantAsync` faqat faollarni aylanadi — shu yetarli).

## Qabul mezoni

- Ikki tenantga ulangan chat `/status` da ikkalasini ko'radi, `/stop` ikkalasini uzadi.
- Ulanmagan chat `/status` → «Hech qaysi profilga ulanmagan» + yo'riqnoma.
- Noma'lum buyruq navbatni to'xtatmaydi (keyingi update qayta ishlanadi).

---

# II qism — bot ish quroli sifatida (TG9–TG17)

## Nega va nima

Mahsulot — muzqaymoq/sut/qandolat zavodlari. Ularda web'ni ochib o'tiradigan odam — ofisdagi
1–2 kishi; qolganlar (sex boshlig'i, ombor mudiri, haydovchi, savdo agenti, do'kon egasi)
kun bo'yi telefonda va Telegram'da. Shu odamlar uchun bot — **ilovaga kirmasdan** eng tez-tez
bajariladigan 5–6 amalni qilish yo'li. Quyidagi ro'yxat loyihadagi mavjud servislarga
qarab tuzilgan: har band uchun backend'da chaqiriladigan metod allaqachon bor, bot faqat
yangi kirish nuqtasi.

| # | Nima | Kim uchun | Qiymat | Hajm | Tayanadi |
|---|---|---|---|---|---|
| **TG9** | Xabardagi tugmalar: transferni tasdiqlash/rad etish, ishlab chiqarishni boshlash | Menejer, direktor | 🔥🔥🔥 | O'rta | TG2, TG3 |
| **TG17** | Platforma egasi (ops) kanali | Siz | 🔥🔥🔥 | Kichik | TG2 |
| **TG11** | Kunlik xulosa + tinch soatlar | Direktor, menejer | 🔥🔥 | O'rta | TG2, TG4 |
| **TG10** | So'rov buyruqlari: `/qoldiq`, `/muddat`, `/qarz`, `/bugun` | Hamma | 🔥🔥 | Kichik–o'rta | TG1, TG3 |
| **TG16** | Tenant guruh chati | Ombor/sex guruhi | 🔥🔥 | O'rta | TG1, TG9 |
| **TG12** | Haydovchi boti: bugungi marshrut, «yetkazildi» tugmasi, yuk xati PDF | Haydovchi | 🔥🔥 (Delivery moduli bor tenantlar) | Katta | TG9 |
| **TG13** | Mijoz (kontragent) boti: yuk chiqdi/yetkazildi, qarz eslatmasi, `/qarzim` | Do'kon, diler | 🔥🔥 | Katta | TG12 dagi «foydalanuvchi bo'lmagan aktor» |
| **TG14** | Davomat va KPI: `/keldim`, `/ketdim`, `/smena`, `/kpi` | Xodim | 🔥 | Kichik | TG10 |
| **TG15** | Hisobot fayllari: `/hisobot` → Excel/PDF | Direktor, buxgalter | 🔥 | Kichik | TG10 |

**Tartib:** TG9 → TG17 → TG11 → TG10 → TG16 → TG14 → TG15 → TG12 → TG13.
TG12/TG13 oxirida: ular «tizim foydalanuvchisi bo'lmagan odam» tushunchasini kiritadi va
Delivery moduli / qarz eslatmasi bo'yicha mijoz roziligi kerak — birinchi real mijoz
so'rasa qilinadi.

### Umumiy dizayn (II qism uchun majburiy)

**Aktor.** Web'da amal egasi `ICurrentUser` (middleware to'ldiradi). Bot scope'ida u bo'sh.
`BotCurrentUser : ICurrentUser` — `WmsAccessContext` ga `IWmsAccessResolver.ResolveAsync(sub, tenantId)`
natijasi qo'yiladi (profil → `identity_sub`), ya'ni ruxsatlar **o'sha RBAC bazasidan,
o'sha kesh bilan**. Servislarga `userId` parametri `profile.Id`. Har tugma/buyruq
**bosilgan paytda** ruxsat qayta tekshiriladi (rol o'zgargan bo'lishi mumkin).

**Audit.** Web'da audit `AuditLogFilter` (MVC filtri) yozadi — bot uni **chetlab o'tadi**.
Bot orqali holat o'zgartiradigan har amal `IAuditService` ni oshkora chaqiradi; `Details`
da `source: telegram`. Buni TG9 da bir marta `BotAction` yordamchisiga o'rang, keyingi
tugmalar shuni ishlatsin.

**Callback.** `callback_data` ≤ 64 bayt: `<amal>:<id N-format>` (masalan `tc:0192…` =
transfer confirm, 35 bayt). Tenant va aktor `callback_data` da **yo'q** — ular
`(chat_id, message_id)` → `telegram_outbox` (TG2 ga `message_id` ustuni qo'shiladi) →
`tenant_id`, `user_profile_id` dan olinadi. Guruhda (TG16) aktor — bosgan odamning
(`from.id`) shu tenantdagi shaxsiy ulanishi; ulanmagan bo'lsa «Avval profilingizni ulang».
Har tugma javobi `answerCallbackQuery` (Telegram 30 s kutadi) va `editMessageText`
(tugmalar olinadi, natija yoziladi: «✅ Tasdiqladi: A. Karimov, 12:03»). Ikkinchi bosish
yoki `xmin` 409 → «Allaqachon tasdiqlangan».

**Tenant tanlash.** Bir chat bir necha tenantga ulangan bo'lsa so'rov buyruqlari (TG10,
TG14, TG15) avval tenantni so'raydi (inline tugmalar) va tanlovni bir soat eslab qoladi:
`wms.telegram_chat_state` (platforma; `chat_id` PK, `tenant_id`, `expires_at`). Bitta tenant —
so'ralmaydi.

**Matn.** Hamma javob `Translations` orqali (TG4 tili). Raqamli jadvallar `<pre>` ichida,
≤ 4096 belgi, ortig'i «… va yana N» bilan kesiladi. Buyruq nomlari lotin o'zbekcha
(`/qoldiq`), `setMyCommands` ikkala tilda tavsif beradi.

**Feature/modul eshigi.** Web'dagi `[RequireModule]`/`[RequireFeature]` bot'da yo'q —
buyruq ishga tushishidan oldin `SubscriptionPolicy` bilan xuddi shu tekshiruv
(`Delivery` moduli yo'q tenantda `/marshrut` → «Tarifingizda yo'q»).

---

# 🟠 TG9 — Xabardagi tugmalar: tasdiqlash

## Muammo

Transfer yaratilganda **hech kim xabar olmaydi** — bildirishnoma faqat tasdiqdan keyin.
Menejer kechqurun ilovani ochib ko'rmaguncha kirim/chiqim «kutilmoqda» da turadi. Eng
ko'p so'raladigan qulaylik: «telefonda tugmani bosib tasdiqlash».

## Ishlar

- Yangi `NotificationType.TransferPending` (15) — `TransferService.CreateAsync` da, qabul
  qiluvchilar TG3 bo'yicha `transfers.confirm`. Matn: tur, kontragent, ombor, summa,
  qatorlar soni, kim yaratdi. Tugmalar: «✅ Tasdiqlash» (`tc:`), «❌ Rad etish» (`tr:`),
  «Ochish» (URL).
- `tc:` → aktor `transfers.confirm` → `ITransferService.ConfirmAsync`; `tr:` →
  `transfers.reject` → `RejectAsync`. Rad etish sababi so'ralmaydi (web'da ham yo'q).
- `ProductionOrder` yaratilganda `ProductionPending` (16) → `production.manage`:
  «▶️ Boshlash» (`ps:`) → `StartOrderAsync`. Bosqichlar (`ExecuteStageAsync` — miqdor
  kiritish kerak) **tugmaga sig'maydi**, web'da qoladi.
- `ITelegramService.SendMessageAsync` ga `replyMarkup` (inline keyboard), yangi
  `AnswerCallbackQueryAsync`, `EditMessageTextAsync`. Polling `allowed_updates` ga
  `callback_query`.
- Outbox'ga `message_id` (yuborilgach) va `reply_markup` (JSON) ustunlari.
- `BotAction` yordamchisi: outbox → scope → `BotCurrentUser` → ruxsat → amal → audit →
  javob. Keyingi TG'lar shuni ishlatadi.

## Qabul mezoni

- Xodim transfer yaratadi → menejer 5 s ichida tugmali xabar oladi → bosadi → web'da
  `Confirmed`, audit jurnalida menejer nomi va `source: telegram`.
- Ruxsati olib qo'yilgan menejer bosganda «Ruxsat yo'q», holat o'zgarmaydi.
- Ikki menejer bir vaqtda bossa — biriga «Tasdiqlandi», ikkinchisiga «Allaqachon», bitta audit yozuvi.
- Web'dan tasdiqlangan transferning Telegram xabari tugmalari **o'chiriladi** (ConfirmAsync'da
  outbox'dagi tegishli `message_id` lar uchun `editMessageReplyMarkup` — outbox qatori
  `notification_id` orqali topiladi; buni ham fon xizmati bajaradi, so'rov ichida emas).

---

# 🟠 TG17 — Platforma egasi (ops) kanali

## Muammo

Yangi tenant kirdi, trial tugayapti, bot xabarlari yiqilyapti, API qayta ko'tarildi —
buni bilish uchun loglarga yoki Console'ga qarash kerak. Bir kishilik operator uchun
Telegram'dagi bitta kanal — eng arzon monitoring.

## Ishlar

- `Telegram:OpsChatId` (config; bo'sh — o'chiq). Sizning shaxsiy chat'ingiz yoki yopiq kanal.
- `IOpsNotifier.SendAsync(text)` → outbox (`tenant_id = null`, `user_profile_id = null`).
- Hodisalar: tenant JIT yaratildi (`WmsPlatformUserSink` birinchi token), trial 7 kun qoldi
  / tugadi / suspend (TG5 dan nusxa), obuna uzaytirildi (`/admin/v1` orqali), outbox'da
  ketma-ket 20+ `Failed`, polling 5 daqiqadan ko'p xato, API start (versiya, muhit).
- Tenant ma'lumoti **yo'q** (mahsulot, summa) — faqat tenant kodi/nomi va hodisa.
- Kunlik bitta qator 09:00: faol tenantlar, kecha yaratilgan transferlar soni (jami),
  outbox `Sent/Failed`.

## Qabul mezoni

- Demo tenantda birinchi kirish → sizga xabar. `OpsChatId` bo'sh — hech narsa, xato yo'q.

---

# 🟠 TG11 — Kunlik xulosa va tinch soatlar

## Muammo

TG2/TG3 dan keyin ham faol zavodda kuniga 30–50 xabar bo'ladi (har partiya, har bosqich).
Kechasi 02:00 da «ishlab chiqarish tugadi» xabari — foydalanuvchi botni bloklaydi, TG2 uzadi,
mijoz «bot ishlamayapti» deydi.

## Ishlar

- Outbox'ga `priority` (`Urgent`, `Normal`). Urgent: `TransferPending`, `LowStock`,
  `BatchExpired`, `SubscriptionWarning`, `Error`. Qolgani Normal.
- **Tinch soatlar** 22:00–07:00 Toshkent (`Telegram:QuietFrom/QuietTo`, tenant sozlamasi
  keyin): Normal xabar `next_attempt_at = ertalab 07:00` bilan yoziladi. Ertalab
  yuboruvchi bir chat uchun ushlab turilganlarni **bitta xabarga** yig'adi («Kechasi: 4 ta
  bosqich tugadi, 2 ta transfer tasdiqlandi» + ro'yxat). Vaqt mintaqasi — hozircha UTC+5
  konstanta (`AnalyticsService` dagi kabi); HOLAT'dagi «vaqt mintaqasi» qarori chiqsa bitta joy.
- **Kunlik xulosa** 08:00: `IAnalyticsService.GetDashboardSummary` + `GetTopDebtors(5)` +
  muddati 7 kun ichida tugaydigan partiyalar soni + kutilayotgan transferlar soni. Qabul
  qiluvchi — `dashboard.view` + o'zi yoqqan (`telegram_link.digest = true`; sukut
  `admin`/`manager` uchun yoqiq, boshqalarga o'chiq). Bo'sh kun (hech narsa bo'lmagan) —
  yuborilmaydi. `dedup_key = "digest:{sana}:{chatId}"`.
- TG7: profilda «Kunlik xulosa» o'chirgichi.

## Qabul mezoni

- 23:00 da yaratilgan `ProductionCompleted` 07:00 da keladi, `LowStock` darhol.
- Kechasi 6 ta hodisa → ertalab bitta xabar.
- Xulosa faqat yoqqanlarga, faqat hodisa bo'lgan kunlarda.

---

# 🟠 TG10 — So'rov buyruqlari

## Ishlar

| Buyruq | Ruxsat | Manba | Javob |
|---|---|---|---|
| `/qoldiq <nom>` | `warehouse.view` | `IAnalyticsService.GetStockLevels(null)` yoki to'g'ridan `WarehouseStocks` (`ILIKE %nom%`, 5 tagacha mahsulot) | Mahsulot → ombor bo'yicha miqdor, min. zaxira belgisi ⚠️ |
| `/muddat` | `warehouse.view` | `Batches` (7 kun, `BatchExpiryService` sharti) | Partiya, mahsulot, sana, qoldiq |
| `/qarz` | `finance.view` | `GetTopDebtors(10)` | Kontragent, summa, eng eski qarz sanasi |
| `/bugun` | `dashboard.view` | `GetDashboardSummary` | Bugungi transferlar (soni/summa), ishlab chiqarish, kam zaxira soni, kutilayotgan tasdiqlar |
| `/kutilmoqda` | `transfers.view` | `Transfers.Status == Pending` | Ro'yxat; `transfers.confirm` bo'lsa har biriga TG9 tugmalari |

- Buyruq argumentsiz `/qoldiq` → «Mahsulot nomini yozing: /qoldiq plombir».
- Bir chat — ko'p tenant → tenant tanlash (umumiy dizayn).
- Javoblar `<pre>` jadval, 4096 chegara.
- Buyruqlar `setMyCommands` ro'yxatiga qo'shiladi (ruxsatga qarab **ko'rsatib bo'lmaydi** —
  Telegram buyruq menyusi chat darajasida; ruxsatsiz buyruq → «Ruxsat yo'q»).

## Qabul mezoni

- `/qoldiq plom` 3 s ichida javob; `viewer` `/qarz` → «Ruxsat yo'q» (`finance.view` bo'lmasa).
- Ikki tenantli chat avval tenantni so'raydi, bir soat qayta so'ramaydi.

---

# 🟠 TG16 — Tenant guruh chati

## Muammo

O'zbekiston biznesida operativ muloqot — Telegram guruhi («Ombor», «Sex»). Shaxsiy
bildirishnoma o'rniga ko'p mijoz «guruhga tashlasin» deydi. I qism qarori «faqat shaxsiy» —
bu yerda oshkora ulash bilan kengaytiriladi.

## Ishlar

- `wms.telegram_group` (platforma): `chat_id`, `tenant_id`, `title`, `types` (TG3 kabi
  nomlar ro'yxati, `null` — hamma), `linked_by_profile_id`, `created_at`. Bir guruh — bir
  tenant; bir tenant — bir necha guruh.
- Admin (`settings.modules`, shaxsiy ulangan) botni guruhga qo'shadi va `/ulash` yozadi →
  bot uning shaxsiy ulanishi orqali tenantni topadi (bir nechta bo'lsa tanlatadi) → guruh
  ulanadi. `/sozlash` → tur tugmalari (yoqish/o'chirish), `/uzish`.
- Broadcast bildirishnoma (`UserId == null`) shaxsiy qabul qiluvchilarga **qo'shimcha**
  guruhga ham ketadi (bitta outbox qatori, `user_profile_id = null`, `chat_id` = guruh).
  Shaxsiy bildirishnoma (`UserId != null`) guruhga **bormaydi**.
- Guruhdagi tugmalar (TG9): aktor — bosgan odam (`from.id`) → shu tenantda shaxsiy ulanishi
  → ruxsat. Ulanmagan bo'lsa `answerCallbackQuery` bilan «Avval profilingizni ulang» (alert).
- So'rov buyruqlari (TG10) guruhda ishlaydi, aktor — yozgan odam. Guruhda `/qarz` ni
  cheklash: faqat `finance.view` bo'lgan odam yozsa; javob guruhga chiqadi — bu admin
  qarori (`/sozlash` da «moliya so'rovlari guruhda» o'chirgichi, sukut o'chiq).
- Bot guruhdan chiqarilsa (`my_chat_member` update) → qator o'chadi.
- I qismdagi «guruh e'tiborsiz» qoidasi: faqat **ulanmagan** guruh e'tiborsiz.

## Qabul mezoni

- Admin `/ulash` → guruh transferlarni oladi; oddiy a'zo `/ulash` → «Faqat administrator».
- Guruhda tasdiqlash tugmasini ulanmagan odam bossa — hech narsa o'zgarmaydi.
- Bot guruhdan chiqarilgach xabarlar to'xtaydi, outbox'da `Failed` to'planmaydi.

---

# 🟡 TG14 — Davomat va KPI

## Ishlar

- `/keldim` → `IKpiService.CheckInAsync` (`AttendanceMethod` ga `Telegram` qiymati;
  `DeviceId = chat_id`). Smena: bugungi `ShiftPlan` dan xodimniki, yo'q bo'lsa tanlatish.
  Ixtiyoriy joylashuv: `request_location` tugmasi — kelsa `Details`/izoh maydoniga
  koordinata (jadvalga ustun **qo'shilmaydi**, v1).
- `/ketdim` → ochiq `AttendanceLog` → `CheckOutAsync`.
- `/smena` → bugungi rejam (`GetPlansAsync(today)` filtrlangan). `/kpi` → shu hafta
  o'z samaradorligim (`GetEfficiencyAsync`, faqat o'zi).
- Feature `kpi.attendance` yo'q tenantda → «Tarifingizda yo'q».
- Menejerga (`kpi.manage`) ertalab 09:00 «Kelmaganlar» xulosasi (TG11 mexanizmi) — reja bor,
  `CheckIn` yo'q xodimlar.

## Qabul mezoni

- `/keldim` ikki marta → «Allaqachon keldingiz (08:02)». `/ketdim` ochiq yozuvsiz → «Avval /keldim».
- Web'dagi davomat sahifasida `Telegram` usuli ko'rinadi.

---

# 🟡 TG15 — Hisobot fayllari

## Ishlar

- `/hisobot` → inline menyu: «Zaxira (Excel)», «Transferlar — shu oy», «Moliya — shu oy»,
  «Kontragentlar». Har biri `IExportService.*Async` → `sendDocument` (multipart; nomlangan
  client orqali, fayl nomi `zaxira-2026-09-11.xlsx`).
- Ruxsat: zaxira `warehouse.view`, transfer `transfers.view`, moliya `finance.view`. Feature
  `export.excel`.
- Katta tenantda fayl 50 MB dan oshsa — «Web'dan yuklab oling» (Telegram bot chegarasi).
- Guruhda (TG16) hisobot **yuborilmaydi** — faqat shaxsiy chatga («Shaxsiy chatga yubordim»).
- Direktorga har dushanba 08:00 haftalik transfer Excel — TG11 xulosasi bilan birga,
  profilda o'chirgich (`telegram_weekly_report`).

## Qabul mezoni

- Fayl brendlangan (`ReportBranding`), web'dagi eksport bilan **bir xil** (bitta servis).

---

# 🟡 TG12 — Haydovchi boti

## Muammo

`Driver` — foydalanuvchi emas (`Phone` bor, hisob yo'q). Marshrutni menejer qog'ozga
yozib beradi, «yetkazildi» ni ofisga qo'ng'iroq qilib aytadi. Bosqich 3 rejasidagi «driver
portal tasdiq» F6 da portal bilan birga o'chgan (D8). Bot — portal o'rniga eng arzon yo'l.

## Ishlar

- `telegram_link.driver_id` (TG1 jadvali; `user_profile_id`/`driver_id`/`counterparty_id`
  dan aynan bittasi). Ulash: haydovchi kartasida «Telegram havolasi» tugmasi →
  `POST /api/delivery/drivers/{id}/telegram-link` → `t.me/<bot>?start=<token>`; token
  `telegram_link_token` da `subject_type = Driver` (`user_profile_id` → `subject_id`).
  Menejer havolani haydovchiga o'zi yuboradi. Muqobil — Wash'dagi «Kontaktni ulashish»
  (`request_contact`, `Driver.Phone` bo'yicha): haydovchi kartasiz ham ulanadi, lekin
  telefon bir necha tenantda bo'lishi mumkin — v1 da faqat havola.
- **Aktor:** haydovchi profil emas — `BotCurrentUser` o'rniga `DriverActor` (tenant scope +
  driver id). `MarkStopDeliveredAsync/MarkStopFailedAsync` `userId` olmaydi — audit
  `Details` da `driverId`.
- Ertalab (yoki `Delivery` `InProgress` bo'lganda): haydovchiga marshrut — to'xtashlar tartib
  bilan (kontragent, manzil, transfer summasi), har biriga «✅ Yetkazildi» / «❌ Yetkazilmadi»
  (sabab — keyingi matn xabari, `ForceReply`). Hammasi yopilgach `UpdateStatusAsync(Completed)`
  (agar servis buni o'zi qilmasa — tekshiring).
- «Yuk xati» tugmasi → `IDeliveryPdfService` → `sendDocument`.
- `Failed` to'xtash → menejerga (`delivery.manage`) darhol xabar (Urgent).
- `/marshrut` — bugungi marshrutni qayta ko'rsatish. Boshqa buyruqlar haydovchiga yopiq.

## Qabul mezoni

- Haydovchi tugma bosgach web'da to'xtash `Delivered`, `DeliveredAt` to'ldirilgan.
- Boshqa tenantning haydovchisi havolasi bilan ulangan chat bu tenant marshrutini **ko'rmaydi**.
- `Delivery` moduli yo'q tenantda havola tugmasi ko'rinmaydi.

---

# 🟡 TG13 — Mijoz (kontragent) boti

## Muammo

Do'kon egasi «yukim qachon chiqadi», «qarzim qancha» deb qo'ng'iroq qiladi. Kontragent
portali F6 da o'chgan (D8). Oziq-ovqat savdosida asosiy og'riq — debitorlik; muntazam,
muloyim eslatma to'lovni tezlashtiradi.

## Ishlar

- `telegram_link.counterparty_id`; ulash — TG12 kabi kartadan havola (`subject_type = Counterparty`).
- **Tenantning o'z boti** — shu yerda qaror qilinadi (Wash §4.2 dalili: do'kon «Agentics»
  degan begona botdan emas, o'zi mol olayotgan zavod nomidan xabar olishi kerak). Kerak
  bo'lsa Wash naqshi to'liq: token UI'da, `ISecretProtector` + `Secrets:MasterKey` (WMS'da
  hozir yo'q), webhook (`Telegram:PublicBaseUrl`, sir sarlavhasi fail-closed), umumiy bot
  dev/demo uchun qoladi. Xodimlar (I qism) baribir umumiy botda — ikki bot bir tenantda.
- **Tenant roziligi:** tenant sozlamasida (Console emas, wms-web Sozlamalar → Bildirishnomalar,
  `settings.modules`) «Mijozlarga Telegram xabarlari» — sukut **o'chiq**; alohida «Qarz
  eslatmasi» (har N kun, N sukut 7, faqat `Debt > 0` va eng eski qarz M kundan katta).
  Sabab: mijozning mijoziga biz xabar yuboramiz — bu tenant qarori, va bir marta noto'g'ri
  sozlansa obro'ga tegadi.
- Xabarlar (Normal, tinch soatlar amal qiladi): unga chiqim tasdiqlandi (+ hujjat PDF
  `ITransferPdfService`), yetkazish yo'lga chiqdi (haydovchi `InProgress` qilganda; to'xtash
  tartibi), yetkazildi, to'lov qabul qilindi (`CreatePaymentAsync`), qarz eslatmasi.
- `/qarzim` → balans va oxirgi 5 ta harakat; `/buyurtmalarim` → oxirgi 5 ta transfer holati.
- Bir do'kon ikki zavoddan olsa — xabarlar tenant nomi bilan (TG4), `/qarzim` tenant tanlatadi.
- Mijozdan **buyurtma qabul qilish** (bot orqali savat) — **qilinmaydi** (v1): narx, qoldiq
  tekshiruvi, tasdiqlash oqimi — bu alohida mahsulot (B2B buyurtma), TZ'da yo'q.

## Qabul mezoni

- Tenant o'chirgichi o'chiq — kontragentga hech narsa ketmaydi, havola tugmasi ham ko'rinmaydi.
- Qarz eslatmasi bir kontragentga N kunda bittadan ko'p emas; qarz 0 bo'lgach to'xtaydi.

---

## Keyinga (bu TZ'da yo'q, lekin yo'l yopilmasin)

| Nima | Nega hozir emas |
|---|---|
| **Telegram Mini App** (wms-web'ni Telegram ichida ochish, `initData` bilan kirish) | Kirish Identity'da (OIDC) — `initData` ↔ Identity sessiyasi almashinuvi platforma ishi, WMS'niki emas. TG10–TG15 buyruqlar 80 % ehtiyojni yopadi |
| **Shtrix-kod rasmi → mahsulot** | Dekoder kutubxonasi + rasm yuklash; avval mahsulotda shtrix-kod maydoni to'ldirilishi kerak |
| **Ovozli/erkin savol («plombir qancha qoldi?»)** | Bosqich 5 AI Advisor bilan birga; TG10 buyruqlari shu bilan almashadi |
| **Bot orqali to'lov (Click/Payme)** | Bosqich 5 billing |
| **Webhook rejimi va tenantning o'z boti** | Bitta umumiy bot, bitta instans; TG13 da qayta ko'riladi (Wash naqshi tayyor) |

---

# Hujjat va qo'llanma (har PR bilan)

- `docs/CLAUDE.md` §2: `Operations` qatoriga «Telegram (polling, outbox)».
- `../docs/qollanma/agentics-wms-qollanma.html` §«Bildirishnoma va Telegram»: `@userinfobot`
  qadamlari o'rniga «Telegram'ga ulash» tugmasi, `/stop`, o'chirgichlar. Bu fayl mijozga
  beriladi — TG7 tugamasdan o'zgartirmang.
- `../agentics-platform/docs/HOLAT.md` §3 «WMS» — har qism yakunida bir qator: qaysi TG'lar bajarildi.
- II qismdan keyin qo'llanmaga alohida bo'lim: buyruqlar jadvali (`/qoldiq`, `/bugun`, …),
  guruh ulash (`/ulash`), haydovchi/mijoz havolasi — kim uchun nima.

# Holat (2026-09-11, branch `telegram-bot`)

| # | Holat | Izoh |
|---|---|---|
| TG8 | ✅ | `TelegramOptions`, `appsettings.json`, `.env.example`, dev/prod compose (`Telegram__BotToken`), `docs/CLAUDE.md` §4. Bot username `getMe` dan |
| TG1 | ✅ | `telegram_link` (RLS) + `telegram_link_token` (RLS yo'q), `user_profile.telegram_chat_id` o'chdi (migratsiya `AddTelegramLinkAndOutbox`). Polling (`TelegramPollingBackgroundService`), `/start <token>`, `my_chat_member` → `is_active=false`, til `language_code` dan. `GET/POST link-token/DELETE /api/me/telegram`. Real botda tekshirildi: ulandi, token yopildi, javob ruscha (Telegram tili `ru`) |
| TG2 | ✅ | `telegram_outbox` (RLS yo'q, `dedup_key` unique), `TelegramOutboxBackgroundService`: tenant bo'yicha scope, 1 xabar/s chat, 429/403/backoff 1-2-4-8 daqiqa, tozalash. `NotificationService` navbatga bildirishnoma bilan bitta `SaveChanges`. Tekshirildi: to'g'ridan qator va `BatchExpiryService` → «Batch Expiring» Telegram'ga yetdi |
| TG7 | ✅ (brauzerda hali ko'rilmagan) | Profil: «Telegram'ga ulash» → havola yangi oynada, 3 s polling `expiresAt` gacha, «Ulangan · @user · sana» + «Uzish». lint/test/build toza. Brauzer sinovi — lokal stack shu branch'dan qayta qurilganda |
| TG3 | ✅ | `NotificationRouting.RequiredPermission`, RBAC bo'yicha qabul qiluvchilar, `muted_types`, `PUT /api/me/telegram/muted`, profilda guruh o'chirgichlari; `ReturnReceived` turi |
| TG4 | ✅ | `message_template`/`message_args` (NotifyAsync) — ilova so'rov tilida, Telegram ulanish tilida; tenant nomi, emoji, «Ochish» (`Telegram:WebUrl`); Guid o'rniga kontragent/ombor + sana. Tekshirildi (ru) |
| TG5 | ✅ | Trial/to'lov muddati va to'xtatish — `settings.modules` egalariga (SubscriptionExpiry 4-qadam), limit 80 % — ilova ichida ham (PlanLimits), dedupe. Tekshirildi |
| TG6 | ✅ | `/status`, `/stop`, `/help`, buyruqlar menyusi (uz/ru) |
| TG9 | ✅ | `TransferPending`/`ProductionPending` tugmali xabarlar (callback_query), aktor = ulanish profili, ruxsat bosilgan paytda, audit `telegram:callback`, tahrir; web'dan bajarilganda tugmalar `RemoveButtons` navbati bilan olib tashlanadi. Real botda tasdiqlandi |
| TG10 | ✅ | `/bugun`, `/kutilmoqda` (tugmalar bilan), `/qoldiq <nom>`, `/muddat`, `/qarz`; ko'p tenantli chatda tanlov (`telegram_chat_state`, 1 soat) |
| TG11 | ✅ | Tinch soatlar 22–07 (shoshilinch bo'lmagan xabar ertalabgacha ushlab turiladi — birlashtirilMAYDI, TZ soddalashdi), kunlik xulosa 08:00 (`digest`, `dashboard.view`), profilda o'chirgich |
| TG14 | ✅ | `/keldim` (`AttendanceMethod.Telegram`, smena joriy vaqtdan yoki tugma), `/ketdim`, `/smena`, `/kpi`; feature va ruxsat buyruq paytida. Ertalabki «kelmaganlar» xulosasi — QILINMADI |
| TG15 | ✅ | `/hisobot` → zaxira/transfer/moliya/kontragent Excel (`IExportService`, `sendDocument`). Haftalik avtomat hisobot — QILINMADI |
| TG16 | ✅ | `telegram_group`: `/ulash` (admin, shaxsiy ulanish orqali), `/sozlash` (tur guruhlari), `/uzish`; umumiy bildirishnomalar guruhga ham; guruhdagi tugma — bosgan odamning shaxsiy ulanishi; kicked/403 → uziladi. So'rov buyruqlari guruhda YO'Q |
| TG17 | ✅ | `Telegram:OpsChatId`: yangi tenant (JIT), obuna hodisalari, API start, polling 5 daqiqa uzilishi, navbatda 5+ xato, kunlik qator 09:00. Tekshirildi |
| TG12 | ✅ (real botda hali sinalmagan) | `telegram_link.driver_id`, kartadan 24 soatlik havola (`delivery/drivers/{id}/telegram-link`), marshrut (yaratilganda, yo'lga chiqqanda, `/marshrut`), ✅/❌ tugmalari faqat o'z yetkazishi, yuk xati PDF, yetkazilmagan → `delivery.manage`, audit `telegram:driver`. «Kontaktni ulashish» — QILINMADI |
| TG13 | ✅ (real botda hali sinalmagan) | `telegram_link.counterparty_id`, kartadan havola, `Tenant.ClientTelegramEnabled` + `DebtReminderDays` (Sozlamalar → Modullar; sukut o'chiq); tasdiqlandi/yo'lda/yetkazildi/to'lov, `/qarzim`, qarz eslatmasi 10:00 N kunda bir. Hujjat PDF mijozga — QILINMADI; tenantning o'z boti — QILINMADI |

Topilgan va tuzatilgan: tokendagi `:` nisbiy URI'ni sxema deb o'qitardi (`NotSupportedException`) — manzil absolyut satr (Wash `0c05a37` bilan bir xil xato); `getMe` o'tmasa polling endi taslim bo'lmaydi, har 60 s qayta uradi. Chat ID (TG17 uchun): egasining shaxsiy chati `806146645`.

Migratsiyalar (tartib bilan): `AddTelegramLinkAndOutbox`, `AddNotificationMessageTemplate`, `AddTelegramOutboxButtons`, `AddTelegramChatState`, `AddTelegramGroup`, `AddTelegramPartners`.

Sinov usuli: alohida `agentics_wms_tg` bazasi (F7 lokal stack'iga tegilmadi), `@AgenticsWmsDevBot`, egasining chati bir vaqtda xodim sifatida ulangan; navbat, tugma, tarjima va ops kanali real botda ko'rildi. Brauzerda (TG7, TG3/TG11 o'chirgichlari, haydovchi/kontragent dialoglari, mijoz sozlamasi) — HALI ko'rilmagan: lokal stack shu branch'dan qayta qurilganda.

# Hisobot

PR tavsifida yoki `docs/TELEGRAM-BOT-HISOBOT.md` da (bitta fayl, bosqichma-bosqich to'ldiriladi):

- Har TG uchun: bajarildi / qisman / qoldirildi va sababi.
- TG4 marshrutlari: qaysi `EntityType` uchun havola bor, qaysi uchun yo'q (wms-web'da sahifa yo'qligi).
- TG3 ruxsat xaritasi kodda qanday bo'ldi (jadval bilan).
- Sinov: real botda (`@BotFather` test tokeni), ikki tenant, ikki foydalanuvchi, bloklash ssenariysi.
  II qismda qo'shimcha: bitta guruh, ruxsati olib qo'yilgan foydalanuvchi bilan tugma, ikki
  menejer bir vaqtda tasdiqlash.
- **Qoida (eski hisobotlardan dars):** «bajarildi» deb yozishdan oldin koddan va real botdan
  tasdiqlang — Bosqich 3 hisoboti Telegram'ni «tugadi» deb yozgan, lekin mijoz uni hech
  qachon ishlata olmagan.
