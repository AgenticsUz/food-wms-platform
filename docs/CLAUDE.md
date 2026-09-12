# CLAUDE.md — Agentics WMS

> **Bu fayl — repo'ning kirish nuqtasi.** Arxitektura qarorlari va sabablari —
> `../agentics-platform/docs/PLATFORMA-TZ.md` §7·F6 (yagona manba); ish qayerda
> to'xtagani — `../agentics-platform/docs/HOLAT.md` §3 «F6». Kod bilan hujjat
> ajralsa, avval hujjat to'g'rilanadi.

## 1. Mahsulot

Oziq-ovqat ishlab chiqaruvchilar (muzqaymoq, sut, qandolat) uchun ombor,
ishlab chiqarish va savdo tizimi — Agentics platformasining mahsuloti (`wms`).
Domen: `wms.agentics.uz`; admin — Console'ning WMS bo'limi (`admin.agentics.uz`).

F6 da (2026-09) .NET 8 + SQLite + o'z JWT'dan **.NET 10 + PostgreSQL 17 (RLS) +
Identity (OIDC)** ga ko'chirildi. Ma'lumot ko'chirilmagan — demo seed.

## 2. Tuzilma

| Joy | Nima |
|---|---|
| `src/WMS.Domain` | Entity'lar (Guid v7, `TenantEntity`) va enum'lar |
| `src/WMS.Application` | DTO, servis interfeyslari, `WmsPermissions`/`WmsSystemRoles`, `SubscriptionPolicy`, tarjimalar |
| `src/WMS.Infrastructure` | `WmsDbContext`, RLS generator/interceptor, migratsiyalar, modul servislari (`Services/<Modul>/`), seed (`Seeding/`) |
| `src/WMS.API` | Host: ikki yuza, JIT, middleware, controller'lar (`Controllers/<Modul>/`, `Controllers/Admin/`) |
| `frontend/` | Nx workspace, `apps/wms-web` (ko'chirish xaritasi — `apps/wms-web/MIGRATION.md`) |
| `seed/demo/tenant.json` | Demo tenant va 4 odam — `agentics-platform/seed/demo/01-identity.sql` bilan MOS bo'lishi shart |
| `docker/` | Dev va prod compose, Dockerfile'lar, konteyner nginx, host vhost |

Modullar (har biri `Add<Modul>Module()`, ulash — `src/WMS.API/Hosting/WmsModules.cs`):
`Catalog` (mahsulot, ombor, zaxira, FEFO), `Trade` (transfer, kontragent, agent,
moliya), `Production` (ishlab chiqarish, QC), `Operations` (yetkazish, KPI,
bildirishnoma, audit, Telegram), `Saas` (plan, feature, obuna, foydalanuvchi/rol,
Console yuzasi), `Reports` (analitika, Excel, PDF brendlash, valyuta).

## 3. Qat'iy qoidalar

1. **Tenant — RLS va global filtr.** Servis tenantni parametr qilib OLMAYDI va
   `TenantId` ni o'zi qo'ymaydi (`StampEntries`). Tenant jadvali — yangi entity
   `TenantEntity` dan meros oladi; RLS siyosati migratsiyada avtomatik.
2. **Ikki yuza, ikki audience.** `/api/*` — wms-web (`wms-api`), `/admin/v1/*` —
   Console (`wms-admin-api`, `wms.admin` yoki platforma admini). Admin yuzasida
   tenant OSHKORA tanlanadi (`UseTenant(id)`).
3. **Foydalanuvchi va parol Identity'da.** WMS profilni JIT yozadi
   (`WmsPlatformUserSink`), odamni tenantga Console biriktiradi. Yirik rol
   (`admin/manager/employee/viewer`) → tizim roli; nozik ruxsatlar WMS'da.
4. **Modul — Identity obunasidan** (token `modules`); plan — narx, limit, feature.
5. **Parallel yozuv:** `xmin` (`WarehouseStock`, `Batch`, `Debt`, `Transfer`,
   `ProductionOrder`, `StageExecution`, `CommissionRecord`) — yutqazgan so'rov 409.
   Balans o'zgarishlari BITTA `SaveChanges`/tranzaksiyada.
6. **Xato shakli:** `/api` — `ApiResponse` + `code` (402/403 kodlari), `/admin/v1` —
   RFC 9457. Xabar — inglizcha shablon = tarjima kaliti (`Translations.cs`,
   dublikat kalit ilovani yiqitadi).
7. 0 xato, 0 ogohlantirish (`TreatWarningsAsErrors`); paket versiyalari —
   `Directory.Packages.props`. Izohlar o'zbekcha, «nega» ni aytadi.

## 4. Buyruqlar

**Deploy** — `docs/DEPLOY.md` (F7 usuli: image lokalda quriladi, SSH orqali `docker load`,
`up --no-build`). Serverda image QURILMAYDI, GitHub `deploy.yml` o'chiq.

```bash
# Build (F6 da avtomatlashtirilgan test YO'Q — foydalanuvchi qarori)
dotnet build AgenticsWms.slnx

# Lokal Postgres (5434) — docker/.env namunadan: cp docker/.env.example docker/.env
docker compose -f docker/docker-compose.yml up -d postgres

# Migratsiya + bazaviy katalog (+ demo) va chiqish
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/WMS.API --no-launch-profile -- seed demo

# API (5511) — Identity 5245 da ko'tarilgan bo'lishi kerak (agentics-platform HOLAT §5)
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5511 dotnet run --project src/WMS.API --no-launch-profile

# Yangi migratsiya
dotnet ef migrations add <Nom> -p src/WMS.Infrastructure -s src/WMS.Infrastructure -o Persistence/Migrations

# Frontend (5510, /api → 5511)
cd frontend && npm install && npx nx serve wms-web
cd frontend && npx nx run-many -t lint,test,build -p wms-web
```

Demo kirish (Identity, parol `Demo@2026!`): `+998901112301` admin, `…02` manager,
`…03` employee, `…04` viewer — `demo` tenanti.

Telegram bot (TZ — `docs/TELEGRAM-BOT-TZ.md`, foydalanuvchi qo'llanmasi —
`docs/TELEGRAM-BOT-QOLLANMA.md`): `docker/.env` da `TELEGRAM_BOT_TOKEN` (bo'sh —
bot o'chiq), `dotnet run` uchun `Telegram__BotToken` env. Dev'da O'Z test botingiz
(`@AgenticsWmsDevBot`) — prod tokeni (`@AgenticsWmsBot`) bilan polling talashadi (409).

⚠️ `dotnet run` bilan ko'tarilgan xizmat `bin/` ni band qiladi — build'dan oldin to'xtating.
⚠️ `--no-launch-profile` SHART, aks holda `launchSettings.json` portni bosib ketadi.

## 5. Bir mijoz uchun fitcha

`CUSTOM_FEATURES.md` — reestr va qoidalar.
