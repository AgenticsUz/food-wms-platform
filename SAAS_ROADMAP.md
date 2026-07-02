# WMS Platform — SaaS gacha Yo'l Xaritasi (Roadmap)

> Strategik reja: hozirgi holatdan to'liq multi-tenant SaaS mahsulotgacha.
> Har bosqich = biznes darvozasi (qachon boshlash) + texnik qamrov (kod bilan asoslangan) + natija.
> Prinsip: **manual → yarim-avtomat → self-service.** Feature'ni birov to'lamasidan oldin ortiqcha qurmang.
>
> **QAROR:** AI Advisor va To'lov (Billing) — **oxirida** bajariladi. Hozir ular uchun faqat *minimal poydevor hook*lari qoldiriladi (pastdagi bo'limga qarang), to'liq qurilish kechiktiriladi.

---

## HOZIRGI ARXITEKTURA (kod bilan tasdiqlangan)

**Allaqachon bor (poydevor tayyor):**
- Multi-tenancy: `Tenant` (Name, Slug, IsActive), har entity'da `TenantId` izolyatsiya.
- Modul tizimi: `Module` + `TenantModule` (tenant bo'yicha modul yoqish/o'chirish), `moduleGuard`.
- RBAC: `Role`, `Permission`, `RolePermission`, `UserRole`, `hasPermission` direktiva + `permissionGuard`.
- `TenantsController` + `TenantService` (tenant boshqaruvi — hozircha admin doirasida).
- Core WMS: Warehouse, Batch, Transfer, Production, Finance, KPI, Counterparty, Product, QC, Shift/Attendance.
- Agent moduli (komissiya), i18n (4 til), analitika (ApexCharts), Excel import/export, PDF, notification, dashboard, barcode, deploy.

**SaaS uchun yetishmayotgani:**
- ❌ SuperAdmin (tenantlar ustidan cross-tenant boshqaruv).
- ❌ Self-service registratsiya / onboarding.
- ❌ Billing / Subscription / Plan / to'lov (monetizatsiya qatlami) — **OXIRGA qoldiriladi**.
- ❌ AI Advisor — **OXIRGA qoldiriladi**.
- ❌ Public landing / narx sahifasi / demo.
- ❌ Operatsion yetuklik: avtomat backup, monitoring, CI/CD.

**Xulosa:** Arxitekturaning ~70% i (izolyatsiya + modul + RBAC) tayyor. Gap — texnik emas, "control plane + go-to-market" qatlami. Billing va AI keyinги bosqichga surildi.

---

## MINIMAL POYDEVOR HOOK'LARI (HOZIR — arzon, kelajakni ochiq qoldiradi)

> Maqsad: AI va to'lovni HOZIR qurmaymiz, lekin keyin ularni ulash uchun qayta-arxitektura shart bo'lmasin.
> Bular kichik, past xarajatли qarorlar — TZ va data-modelga shu bugun kiritilsin.

**To'lov / Billing uchun (minimal):**
- `Tenant` ga nullable maydonlar rezerv qil: `PlanType` (string?, masalan "trial"/"basic"/null) va `SubscriptionStatus` (enum? Trial/Active/Suspended, default Active). Hozir hech qaerда majburlanmaydi — faqat mavjud bo'lsin.
- Modul gating allaqachon `TenantModule` orqali ishlaydi → keyin plan→modul mapping shu tizimga ulanadi, yangi arxitektura kerak emas.
- Sotuv/transfer tarixi o'chirilmasin (billing hisobi keyin shu data ustidan ishlaydi).
- **Qurilmaydi hozir:** Plan/Subscription/Invoice entity to'liq, CLICK/Payme integratsiya, limit enforcement, trial oqimi.

**AI Advisor uchun (minimal):**
- Analitika endpoint'lari toza va barqaror bo'lsin (AI keyin shulardan o'qiydi — yangi data pipeline shart emas).
- Tarixiy data saqlansin: sotuv, ishlab chiqarish, chiqindi, batch muddati (AI bashorati uchun xom ashyo).
- (Ixtiyoriy) `IAiAdvisorService` interfeys stub — bo'sh, implementatsiyasiz. Faqat kelajak joyini belgilaydi.
- **Qurilmaydi hozir:** Claude API integratsiya, bashorat logikasi, tavsiya UI.

> Bu hook'lar TZ'da "reserved / future" deb belgilanadi. Kod hajmi ~1 kunlik, lekin keyingi integratsiyani bir necha hafta yengillashtiradi.

---

## BOSQICH 1 — Mahsulot to'liqligi (bitta real mijoz sotuvga tayyor)

**Darvoza:** HOZIR. Mijoz so'rovlari bor. Maqsad — 1 ta real mijoz butun operatsiyasini shu tizimda yuritsin.

| Vazifa | Holat | Manba |
|---|---|---|
| Agent moduli frontend | ✅ tugadi | — |
| Return / Qaytarish | ✅ tugadi (backend + frontend to'liq ulangan) | mijoz + domen |
| Audit Log | ✅ tugadi (global filter + `/api/audit` + settings sahifasi, 2026-07-02) | mijoz aniq so'ragan |
| Batch Edit (admin) | ✅ tugadi (`batches` komponentida edit) | mijoz so'ragan |
| Notification navigatsiya | ✅ tugadi (`navigateToEntity` Transfer/Batch/Production/Product) | Batch/Production'ga o'tish |
| Delivery / yuk xati | shartли | mijoz talab qilsa |
| Billing/AI minimal hook'lar | ✅ tugadi (`Tenant.PlanType`+`SubscriptionStatus`, `IAiAdvisorService` stub, 2026-07-02) | kelajakni ochiq qoldirish |

**Natija:** Birinchi real mijoz jonli ishlaydi. Bu — eng qimmatli validatsiya. Shu bosqichда to'xtab, mijozni kuzatib, feedback yig'ing.

**Strategik eslatma:** Keyingi bosqichga o'tishdan oldin **1–2 mijozni qo'lda (manual) onboard qiling** — DB'ga o'zingiz tenant kiritib. To'lovни ham hozircha manual (naqd/o'tkazma) yuriting. SuperAdmin qurishdan oldin mahsulot kerakligiga ishonch hosil bo'lsin.

---

## BOSQICH 2 — SaaS boshqaruv qatlami (Control Plane) — ✅ TUGADI (2026-07-02)

> Batafsil hisobot: `BOSQICH2_CONTROL_PLANE_REPORT.md`. Bajarildi: tenant izolyatsiya audit
> (jiddiy leak yo'q, 5 FK-bo'shliq yopildi), SuperAdmin (User.IsSuperAdmin + policy +
> TenantsController), self-service `POST /api/auth/register`, frontend register sahifasi +
> `/superadmin/tenants` boshqaruv UI. Ochilishdan oldin: rate limiting + slug qora ro'yxat tavsiya.

**Darvoza:** 1–2 manual mijoz mahsulotni tasdiqlagach. Endi qo'lsiz tenant qo'shish kerak.

**Texnik qamrov:**
- **SuperAdmin rol + scope:** JWT'ga `SuperAdmin` claim, `[Authorize(Policy="SuperAdmin")]`. SuperAdmin tenant filtridan chetda — barcha tenantlarni ko'radi.
- **Self-service registratsiya:** `POST /api/auth/register` → Tenant + Admin user + default modullar + (ixtiyoriy) demo data yaratadi, credential qaytaradi.
- **platform-admin-ui** (alohida Angular app):
  - Public: `/` landing, `/pricing`, `/register` (onboarding form).
  - Private (SuperAdmin): `/admin/dashboard` (platforma statistikasi), `/admin/tenants` (ro'yxat, yaratish, tahrirlash, modul toggle, soft-delete).
  - JWT tenant JWT'dan alohida saqlanadi.
- **Tenant izolyatsiya audit (KRITIK):** registratsiya ochilishidan oldin har endpoint TenantId bo'yicha filtrlanganini tekshiring. Bitta izolyatsiya bug'i = boshqa mijoz ma'lumoti oshkor bo'lishi. Bu bosqichning eng muhim xavfsizlik ishi.

**Natija:** Yangi mijoz o'zi ro'yxatdan o'tadi, siz admin paneldan boshqarasiz. Manual ish tugaydi. To'lov hali manual.

---

## BOSQICH 3 — O'sish (Growth, AI va to'lovsiz) — ✅ (Telegram + Delivery tugadi, 2026-07-02)

> Batafsil: `BOSQICH3-4_REPORT.md`. Telegram bildirishnomalar (config-gated) va Delivery moduli
> (transport/haydovchi/yetkazish/waybill PDF, backend + frontend) bajarildi. Public marketing
> sayt qoldirildi (dizayn/kontent ishi).

**Darvoza:** Control plane ishlagach, bir necha aktiv mijoz bo'lganda.

- **Telegram bot:** notification'lar (low stock, transfer, batch muddati, production). `@BotFather` token, user o'z chat_id sini ulaydi. `TelegramService.SendMessage` mavjud `NotificationService.CreateAsync` yonida chaqiriladi.
- **Delivery moduli:** Vehicle + Driver, marshrut (1 mashina, N mijoz), yuk xati PDF, driver portal tasdiq.
- **Public marketing sayt:** landing, narx (hali "biz bilan bog'laning"), demo, blog/SEO (food WMS bo'yicha O'zbek tilida kontent — organik lead).

**Natija:** Mahsulot ko'proq mijozga yetadi, kundalik qulaylik oshadi. Monetizatsiya hali manual, AI hali yo'q — lekin poydevor hook'lari joyida.

---

## BOSQICH 4 — Miqyos va operatsion yetuklik (Scale) — ✅ (asosiy qism tugadi, 2026-07-02)

> Batafsil: `BOSQICH3-4_REPORT.md`. Avtomat kunlik DB backup (VACUUM INTO snapshot), health
> check (`/health`) + structured logging (Serilog), CI/CD (GitHub Actions), performance
> indekslar bajarildi. Sentry/off-site backup — config-hook qoldirildi (tashqi kalit kerak).
> DB-per-tenant qarori hozircha shart emas (shared DB + TenantId).

**Darvoza:** Mijoz soni oshib, manual operatsiya to'siq bo'la boshlaganda.

- **Avtomat backup + restore mashqi:** kunlik DB backup, off-site, oyda bir restore sinovi. *(sarash.uz'dagi SQLite yo'qotish hodisasi — bu qat'iy intizom talab qiladi.)*
- **Monitoring / alerting:** uptime, xato kuzatuvi (Sentry), health-check, disk/CPU alert.
- **Performance audit:** DB indekslar, pagination, N+1 tekshiruvi, kerak bo'lsa caching (Redis).
- **CI/CD:** deploy avtomatlashtirilgan (hozir manual `dotnet publish` + `ng build` + scp).
- **Onboarding avtomatlashtirish:** demo, dokumentatsiya, in-app tur, support kanali.
- **Arxitektura qarori:** hozir shared DB + TenantId. 50+ tenant / yirik mijoz kelsa — DB-per-tenant yoki schema-per-tenant ni ko'rib chiqing (hozir shart emas, lekin bilib turing).

**Natija:** Platforma ishonchli, kuzatiladigan, o'zi kengayadigan bo'ladi.

---

## BOSQICH 5 — OXIRGI: Monetizatsiya (Billing) + AI Advisor

**Darvoza:** Barqaror to'lovchi baza (mijozlar pul to'lashga tayyor va soni manual to'lovни qiyinlashtirганда) + data tarixi to'plangач (AI uchun).

### 5A — Billing (avtomat to'lov)
Bosqich 1'dagi hook'lar ustiga quriladi (Tenant.PlanType / SubscriptionStatus allaqachon bor).
- **`Plan`** (Basic / Pro / Enterprise): narx, modul to'plami, limitlar (max users, warehouses, transfers/oy).
- **`Subscription`** (tenant bo'yicha): planId, status, trialEndsAt, currentPeriodEnd.
- **`Invoice`** / to'lov tarixi.
- **Plan asosida modul gating:** TenantModule endi Subscription bilan boshqariladi.
- **Limit enforcement:** middleware — limit oshsa 402/ogohlantirish.
- **To'lov integratsiyasi:** CLICK / Payme (UZEX Marketplace'dagi CLICK tajribasiga o'xshaydi). Invoice generatsiya.
- **Trial oqimi:** 14 kun bepul → grace → to'lanmasa suspend (data saqlanadi).

### 5B — AI Advisor (farqlovchi qism)
Bosqich 1'dagi toza analitika endpoint'lari va tarixiy data ustiga quriladi.
- Claude API (`claude-sonnet-4-6`): sotuv tarixiga qarab zaxira bashorati, ishlab chiqarish rejasi, chiqindi kamaytirish tavsiyasi.
- **Bu — oziq-ovqat domeningiz + AI = raqobatchilar takrorlay olmaydigan qism.** Shuning uchun oxirida, lekin alohida e'tibor bilan quriladi.

**Natija:** Pul avtomatik kelib tushadi va mahsulot generic WMS'dan food-specific AI-powered platformaga aylanadi.

---

## STRATEGIK ESLATMALAR (halol tahlil)

1. **Poydevor sizda bor — asosiy gap monetizatsiya emas, hozir mahsulot to'liqligi va mijoz.** Feature'larni tugatib, real mijozga chiqing. Billing/AI keyin.

2. **AI + to'lovni oxirga surish — MVP uchun to'g'ri qaror.** Ammo halol ogohlantirish: biznes nuqtai nazaridan billing odatda kattaroq o'sishdan OLDIN keladi. Manual to'lov 5–10 mijozgacha ishlaydi; undan oshsa, Bosqich 5A'ni kechiktirmang, aks holda pul yig'ish o'zi to'siqqa aylanadi.

3. **Minimal hook'lar shuning uchun muhim:** hozir Tenant'ga 2 ta nullable maydon + toza analitika endpoint qo'shsangiz, keyin billing/AI'ni ulash qayta-arxitektura talab qilmaydi. Bu — kechiktirishning "xavfsiz" usuli.

4. **Ketma-ketlik xavfi:** SuperAdmin'ni 1–2 manual mijoz validatsiyasidan OLDIN qurmang. Hech kim istamaydigan SaaS qurib qo'yish — eng keng tarqalgan xato.

5. **Tenant izolyatsiya = №1 texnik xavf.** Registratsiya ochilishidan oldin har endpoint TenantId bo'yicha filtrlanishini audit qiling.

6. **Sizning mudofaa devoringiz — food domeni, generic WMS emas.** AI Advisor oxirida bo'lsa ham, u sizning asosiy farqlovchi qismingiz. Shuning uchun uni butunlay unutmang — hook'larini bugun qoldiring, data tarixini saqlang.

7. **Data intizomi bugundan.** Backup avtomatlashtirilgan bo'lsin — Bosqich 4'ni kutmang.

8. **Land-and-expand:** BMB kabi yirik mijozga avval kichik pilot, keyin modul-modul kengaytirish.

---

## QISQA KETMA-KETLIK (bir qatorda)

```
[HOZIR hook'lar: Tenant.PlanType/SubscriptionStatus + toza analitika endpoint]
Return → Audit Log → Batch Edit            [Bosqich 1: mahsulot to'liqligi]
   → 1-2 manual mijoz (validatsiya, manual to'lov)
   → SuperAdmin + register + platform-admin-ui + izolyatsiya audit   [Bosqich 2: control plane]
   → Telegram + Delivery + marketing sayt   [Bosqich 3: o'sish, AI/to'lovsiz]
   → backup + monitoring + CI/CD + performance   [Bosqich 4: miqyos]
   → Billing (Plan/Subscription/CLICK/Payme) + AI Advisor   [Bosqich 5: OXIRGI]
```

> Har bosqich mustaqil deploy qilinadi. Bosqich ichidagi vazifalar uchun alohida Claude Code prompt fayllari (masalan `RETURN_PROMPTS.md`) tuziladi.
> AI va to'lov — oxirgi bosqich, lekin poydevor hook'lari Bosqich 1'da qoldiriladi.
