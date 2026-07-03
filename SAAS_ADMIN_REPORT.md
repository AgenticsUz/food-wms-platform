# SaaS Control Plane — Alohida Admin ilova (wms-admin) hisoboti

> **Sana:** 2026-07-03 · **Branch:** `saas-admin` · **Repo:** `golibjon94/food-wms-platform`

Bu hujjat SaaS boshqaruvini alohida platform-admin ilovasiga (`wms-admin`) ko'chirish va
to'liq SaaS MVP darajasiga olib chiqish bo'yicha bajarilgan ishlarni yozadi.

**Qamrov:** CLICK/Payme (avtomat to'lov), Telegram bot va AI — **kiritilmadi** (kelishuv bo'yicha).
SaaS MVP: platform admin barcha imkoniyatlarni SuperAdmin ilovadan boshqaradi, obuna
enforcement bilan (manual to'lov + suspend).

---

## Arxitektura — ikki alohida ilova

| Ilova | Kim uchun | Port | Nima |
|---|---|---|---|
| **wms-ui** (mavjud) | Zavod xodimlari (tenant) | 7050 | Ombor/ishlab chiqarish/moliya/delivery — operatsion WMS |
| **wms-admin** (yangi) | Platforma egasi (SuperAdmin) | 7060 | Tenantlar, planlar, statistika — control plane |

Ikkalasi bitta backend'ni (`wms-api`) ishlatadi. Tenant app `/api/*`, admin app `/api/admin/*`.
Admin app faqat `IsSuperAdmin` foydalanuvchilarni kiritadi (login oddiy tenant adminini rad etadi).

---

## Bajarilgan ishlar

| Vazifa | Commit |
|---|---|
| wms-admin scaffold (alohida Angular ilova, login, shell, sahifalar) | `255a634` |
| wms-ui'dan superadmin bo'limini olib tashlash | `b3662ec` |
| Backend: Plan + obuna enforcement + `api/admin/*` | `7234a42` |

---

## 1. wms-admin — alohida Angular ilova

- Angular 21 (standalone, signals, zoneless), PrimeNG 21 (Aura), ApexCharts.
- **Dizayn tizimi wms-ui bilan bir xil** (`styles.scss` nusxalandi) — vizual izchillik.
- Inglizcha (ichki admin konsol — Transloco'siz, sodda).
- **Login** — SuperAdmin-only (oddiy admin → "platform administrators only" rad).
- **Shell** — sidebar (Dashboard / Tenants / Plans) + user/logout.
- **Dashboard** — platform statistikasi: KPI kartalar (jami/active/trial/suspended tenant,
  jami user, oylik yangi), tenant o'sishi grafigi (ApexCharts), so'nggi tenantlar.
- **Tenants** — ro'yxat, yaratish (to'liq provizatsiya + admin hisobi + plan), tahrirlash
  (plan/status/trial/faollik), **suspend/activate**, plan biriktirish, modul toggle, o'chirish.
- **Plans** — CRUD: nom, kod, narx, modul to'plami (multiselect), limitlar (max users/warehouses/transfers).
- Prod build toza (budjet oshmadi) — deploy uchun tayyor.

## 2. wms-ui tozalash

- `modules/superadmin/`, superadmin guard/service, `/superadmin/tenants` route, sidebar "Platform"
  entry olib tashlandi — endi hammasi wms-admin'da.
- Self-service registratsiya (`/auth/register`) wms-ui'da **qoldi** — bu tenant-app ishi
  (zavod egasi o'zi ro'yxatdan o'tadi).

## 3. Backend — control plane

- **`Plan` entity** (platform-global): nom, kod, narx, modul to'plami (CSV), limitlar
  (maxUsers/maxWarehouses/maxTransfersPerMonth). `PlanService` CRUD (bog'langan tenant bo'lsa
  o'chirish bloklanadi). `Tenant.PlanId` + `TrialEndsAt`. Migration `AddPlansAndSubscription`.
- **Plan → modul gating:** planni tenantga biriktirsa, tenantning modullari aynan plan
  to'plamiga tenglashtiriladi (`PlanModules.ApplyPlanModulesAsync`).
- **Obuna enforcement (asosiy SaaS bo'g'ini):** login'da `Suspended` tenant va muddati o'tgan
  `Trial` bloklanadi (`AppException`). SuperAdmin bundan mustasno.
- **`AdminController`** (`api/admin`, `[Authorize(Policy="SuperAdmin")]`): tenants (CRUD +
  suspend/activate + assign-plan + modules), plans (CRUD), modules, stats.
- CORS'ga `localhost:7060` qo'shildi.

### `api/admin/*` (barchasi SuperAdmin)
```
GET/POST/PUT/DELETE  tenants[/{id}]
PUT  tenants/{id}/suspend | /activate | /plan
GET/PUT  tenants/{id}/modules
GET/POST/PUT/DELETE  plans[/{id}]
GET  modules | stats
```

---

## Sinov (haqiqiy server, uchma-uch)

1. Superadmin login → `isSuperAdmin=True`; `admin/modules` → 9 modul.
2. Plan yaratish (Basic = WAREHOUSE_RAW + TRANSFERS, limitlar bilan) → OK.
3. Plan bilan tenant yaratish → tenant modullari **faqat plandagilar** (WAREHOUSE_RAW, TRANSFERS)
   yoqildi — plan gating ishlaydi. Yangi tenant admini login → 2 modul.
4. **Suspend** → tenant admini login **400 (bloklandi)**; superadmin bypass; **Activate** → login tiklandi.
5. `admin/stats` → total=2, trial=1, users=2, 12 oylik o'sish.

Backend build **0/0**, wms-admin dev + prod build **toza**.

---

## SaaS MVP holati

Endi platforma:
- ✅ Tenantlarni SuperAdmin ilovadan to'liq boshqaradi (yaratish/tahrirlash/suspend/o'chirish).
- ✅ Planlar (modul to'plami + limitlar) — plan tenant modullarini boshqaradi.
- ✅ **Obuna enforcement** — to'lamagan tenantni suspend qilib uzish ishlaydi (manual billing real).
- ✅ Self-service registratsiya + platform statistikasi.

**Manual billing bilan ishga tushirishga tayyor** (5–10 mijozgacha).

## Keyingi (kelishuv bo'yicha kechiktirilgan)
- Avtomat to'lov (CLICK/Payme) — to'lovchi baza to'planganda.
- Limit enforcement (maxUsers/warehouses/transfers oshsa 402/ogohlantirish) — plan limitlari
  hozir saqlanadi, lekin hali majburlanmaydi. Keyingi qadam sifatida oson qo'shiladi.
- Telegram bot / AI Advisor.

## Deploy eslatmasi
- wms-admin alohida subdomenga (masalan `admin.domain.uz`) chiqariladi, `/api` backendga
  proxy. wms-ui va wms-admin — alohida hostlar.
- Prod'da `Jwt:Key` va admin parolni env orqali bering (avval aytilgan hardening).

## Commitlar
```
7234a42  Add SaaS control-plane backend: plans, subscription enforcement, api/admin
b3662ec  Remove superadmin section from wms-ui (moved to wms-admin)
255a634  Scaffold wms-admin — separate SaaS platform admin app
```
