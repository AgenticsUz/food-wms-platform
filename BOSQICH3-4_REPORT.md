# Bosqich 3–4 + Hardening: Bajarilgan ishlar hisoboti

> **Sana:** 2026-07-02 · **Branch:** `new-style` · **Repo:** `golibjon94/food-wms-platform`
> **Manba reja:** `SAAS_ROADMAP.md`

Bu sessiyada roadmapning **Bosqich 3 (o'sish)** va **Bosqich 4 (miqyos/operatsion yetuklik)**
dan barcha kod bilan bajariladigan ishlar, hamda ochilish-oldi hardening bajarildi.
**Bosqich 5 (Billing + AI) ataylab qoldirildi** — reja bo'yicha oxirgi bosqich.

Har vazifa build (0 xato / 0 warning) va haqiqiy server sinovidan o'tib, alohida commit qilindi.

---

## Umumiy natija

| Vazifa | Bosqich | Holat | Commit |
|---|---|---|---|
| Rate limiting + slug qora ro'yxat | Pre-launch | ✅ | `87cc5fe` |
| Health check + structured logging (Serilog) | 4 | ✅ | `91b3feb` |
| CI/CD (GitHub Actions) + DB performance indekslar | 4 | ✅ | `91ca394` |
| Avtomat kunlik DB backup | 4 | ✅ | `6459524` |
| Telegram bildirishnomalar | 3 | ✅ | `34fb40b` |
| Delivery moduli (backend + frontend) | 3 | ✅ | `1ec42f6` |

---

## 1. Ochilish-oldi hardening (`87cc5fe`)

Public registratsiya (`POST /api/auth/register`) uchun:
- **Rate limiting** (.NET built-in fixed-window, IP bo'yicha): `/api/auth/login` va
  `/register` daqiqasiga 10 so'rov → ortig'iga **429**.
- **Slug qora ro'yxati + format**: `admin`, `api`, `www`, `portal`, `superadmin`, `billing`
  va h.k. zaxira; slug faqat kichik harf/raqam/tire (3–50 belgi). Noto'g'ri/band → 400.

**Sinov:** `admin` slug → 400; `My Zavod!` → 400; to'g'ri slug → OK; 15 login urinish → 429 chiqdi.

---

## 2. Health check + structured logging (`91b3feb`)

- **Serilog:** konsol + kunlik aylanuvchi fayl (`logs/wms-*.log`, 14 kun), request logging.
- **Health check:** `GET /health` DB ulanishini tekshiradi (`AddDbContextCheck`) — load
  balancer / uptime monitoring uchun. `"Healthy"` qaytaradi.
- `.gitignore`: `logs/`, `backups/`.
- Sentry keyin `Sentry.AspNetCore` + `Sentry:Dsn` bilan bir qatorda ulanadi (hook qoldirilgan).

**Sinov:** `/health` → 200 "Healthy"; Serilog fayli structured yozuvlar bilan yaratildi.

---

## 3. CI/CD + performance indekslar (`91ca394`)

- **GitHub Actions** (`.github/workflows/ci.yml`): push/PR da backend (dotnet Release) va
  frontend (npm ci + ng build production) qurilishi.
- **13 EF indeks** (barchasi tenant-scoped hot query'lar): Transfer (TenantId,Status)/
  (TenantId,ConfirmedAt), WarehouseStock (TenantId,WarehouseId,ProductId), Batch
  (TenantId,ProductId)/ExpiryDate, Transaction (TenantId,Date), Product/Counterparty
  (TenantId), ShiftPlan/ShiftActual (TenantId,Date), Notification (TenantId,UserId,IsRead),
  CommissionRecord (TenantId,AgentId). Migration `AddPerformanceIndexes`.

**Sinov:** migration startup'da qo'llandi, server sog'lom.

---

## 4. Avtomat kunlik DB backup (`6459524`)

- **`DbBackupBackgroundService`:** SQLite'dan izchil snapshot (`VACUUM INTO` — WAL bilan
  xavfsiz), `backups/wms-<timestamp>.db`, oxirgi N nusxa saqlanadi.
- Sozlamalar (`Backup`): Enabled, Directory, IntervalHours (24), Retention (14).
- Off-site (S3/rsync) — deploy skriptida hook.
- *(Eslatma: roadmapdagi "sarash.uz SQLite yo'qotish" saboqiga javob — data intizomi bugundan.)*

**Sinov:** startup'dan ~1 daqiqa keyin `wms-<ts>.db` (503 KB) snapshot yaratildi.

---

## 5. Telegram bildirishnomalar (`34fb40b`)

- **`TelegramService`:** Telegram Bot API orqali xabar. `Telegram:BotToken` sozlanmagan
  bo'lsa (default) — **butunlay no-op** (hech qanday tashqi chaqiruv, hech qanday overhead).
- **`NotificationService`** har yaratilgan bildirishnomani tegishli foydalanuvchi(lar)ning
  Telegram'iga uzatadi (user-specific → o'sha user; broadcast → chat ulagan tenant userlari),
  best-effort, faqat bot yoqilganda.
- **`User.TelegramChatId`** (migration); `PUT /api/auth/telegram` ulash/uzish; `/me` da qaytadi.
- **Frontend:** profil sahifasida Telegram bo'limi (chat ID + saqlash), i18n. Chat ID ni
  foydalanuvchi @userinfobot dan oladi.

**Sinov:** chat ID saqlanadi, `/me` aks ettiradi; token yo'qligida bildirishnoma regressiyasiz
ishlaydi (telegram no-op).

---

## 6. Delivery moduli (`1ec42f6`) — eng katta feature

Permission-gated (`delivery.view` / `delivery.manage`, perms 26/27).

**Backend:**
- Entitylar: `Vehicle`, `Driver`, `Delivery`, `DeliveryStop` (+ enum `DeliveryStatus`,
  `DeliveryStopStatus`). Migration `AddDeliveryModule`.
- `DeliveryService`: transport/haydovchi CRUD; yetkazish yaratish (transport/haydovchi/
  kontragent/transfer tenant'ga tegishliligini tekshiradi, ≥1 manzil); status o'zgartirish;
  har manzil deliver/fail (Planned→InProgress→Completed avtomat o'tadi).
- `DeliveryPdfService`: yuk xati (waybill) PDF (QuestPDF).
- `api/delivery/*` + `GET {id}/waybill`.

**Frontend** (`/delivery`, sidebar "Delivery" guruhi):
- Yetkazishlar ro'yxati (status filtri), yetkazish yaratish (transport/haydovchi/sana +
  manzillar konstruktori), yetkazish detali (info, status amallari, har manzil deliver/fail,
  waybill yuklab olish), transport va haydovchilar boshqaruvi.
- `delivery.service` + model + status helperlar; uz/ru/en/uz-cyrl i18n.

**Sinov (uchma-uch):** transport/haydovchi/yetkazish yaratildi (2 manzil) → start → 1-manzil
yetkazildi (1/2) → waybill PDF (200, application/pdf, 66 KB).

---

## Ataylab qoldirilgani

- **Bosqich 5 — Billing (Plan/Subscription/Invoice/CLICK/Payme) + AI Advisor:** roadmap bo'yicha
  eng oxirgi bosqich. Poydevor hook'lari (`Tenant.PlanType`/`SubscriptionStatus`,
  `IAiAdvisorService` stub) allaqachon joyida.
- **Public marketing sayt (landing/pricing/blog):** asosan dizayn/kontent ishi, kod qiymati
  past — alohida bosqichda (yoki alohida sayt sifatida) qilinadi.
- **Sentry / off-site backup:** tashqi hisob/kalit talab qiladi — config-hook qoldirildi
  (Sentry: bir qatorlik integratsiya; backup: deploy-side S3 hook).

---

## Commit tarixi (bu ish)

```
1ec42f6  Add Delivery module (vehicles, drivers, deliveries, waybill)
34fb40b  Add Telegram notification forwarding (config-gated)
6459524  Add automated daily database backup
91ca394  Add CI workflow + database performance indexes
91b3feb  Add health check + structured logging (Serilog)
87cc5fe  Harden auth: rate limiting + slug blacklist/format
```

(Avvalgi bog'liq: `dced2da` Audit Log + hook'lar; `46b32f5`/`a8dcc98`/`0e29552` Control Plane.)
