# Agent (Vositachi / Sotuv agenti) Moduli — Ish Rejasi

> Yaratilgan: 2026-06-24
> Asos: `NEXT_TASKS.md → Phase 4.2 (Agent & Delivery modules)` + mavjud arxitektura tahlili
> Holat: **Reja** (boshlanmagan)
> Ko'lam: Faqat **Agent** moduli. Delivery (yetkazib berish) keyingi bosqichga qoldiriladi.

---

## 1. MAQSAD

Zavod mahsulotini mijozlarga **vositachi (agent)** orqali sotish imkonini qo'shish.
Har sotuvda ikki yo'l bo'ladi:
- **To'g'ridan-to'g'ri** — zavod ↔ mijoz (hozirgi oqim, o'zgarmaydi)
- **Agent orqali** — zavod → agent → mijoz, agentga **komissiya** hisoblanadi

Agent o'z sotuvlari va komissiyasini ko'rishi uchun alohida imkoniyat oladi.

---

## 2. QARORLAR VA TAXMINLAR (boshlashdan oldin tasdiqlanadi)

> Quyidagilar TZ'da aniqlanmagan. Pastdagi **default** qarorlar bilan boshlanadi;
> mijoz/biznes talabiga ko'ra o'zgartirilishi mumkin.

| # | Savol | Default qaror (tasdiqlanadi) |
|---|---|---|
| D1 | Agent qayerga kiradi? | **Asosiy tizimga "Agent" roli bilan** + qo'shimcha **agent portali** (read-only). Avval CRUD + rol, portal — 2-bosqich. |
| D2 | Mijoz agentga qanday bog'lanadi? | Mijozda doimiy `AgentId` (ixtiyoriy). Sotuvda default shu agent qo'yiladi, lekin **har sotuvda agentni o'zgartirish/olib tashlash mumkin**. |
| D3 | Tanlov qayerda? | Sotuv (Outgoing transfer) yaratishda: "Agent orqali" toggle → agent tanlash maydoni ochiladi. |
| D4 | Komissiya nimadan? | **Sotuv summasidan** (`TransferItem` lar yig'indisi = TotalPrice). |
| D5 | Komissiya qachon hisoblanadi? | Transfer **Confirmed** bo'lganda yoziladi (snapshot: foiz va summa saqlanadi). |
| D6 | Komissiya foizi qayerda? | Har agent uchun yagona `CommissionPercent`. Sotuvda qo'lda override qilish mumkin. |
| D7 | Komissiya to'lovi kuzatiladimi? | Ha — agentga to'langan/qolgan komissiya. Finance'ga ixtiyoriy `Expense` sifatida bog'lanadi. |

**⚠️ D4/D5 — eng muhim.** Agar komissiya "to'langan puldan" hisoblanishi kerak bo'lsa (Confirmed emas, balki Payment bo'lganda), mantiq o'zgaradi — tasdiqlang.

---

## 3. BACKEND (wms-api)

### 3.1 Domain — yangi/o'zgargan entity'lar

**Yangi: `Agent.cs`**
```csharp
public class Agent : BaseEntity
{
    public int TenantId { get; set; }
    public int? UserId { get; set; }          // tizimga kiruvchi akkaunt (ixtiyoriy)
    public User? User { get; set; }
    public string Name { get; set; }
    public string? Phone { get; set; }
    public double CommissionPercent { get; set; }   // 0..100
    public bool IsActive { get; set; } = true;
    // Portal (2-bosqich)
    public string? PortalPhone { get; set; }
    public string? PortalPasswordHash { get; set; }
    public bool PortalEnabled { get; set; } = false;
}
```

**Yangi: `CommissionRecord.cs`** (har sotuvdan hisoblangan komissiya)
```csharp
public class CommissionRecord : BaseEntity
{
    public int TenantId { get; set; }
    public int AgentId { get; set; }
    public Agent Agent { get; set; }
    public int TransferId { get; set; }
    public Transfer Transfer { get; set; }
    public double SaleAmount { get; set; }        // sotuv summasi (snapshot)
    public double CommissionPercent { get; set; } // qo'llangan foiz (snapshot)
    public double CommissionAmount { get; set; }  // hisoblangan komissiya
    public bool IsPaid { get; set; } = false;
    public DateTime? PaidAt { get; set; }
}
```

**O'zgartirish: `Counterparty.cs`** — `public int? AgentId { get; set; }` qo'shiladi (mijozning doimiy agenti).

**O'zgartirish: `Transfer.cs`** — `public int? AgentId { get; set; }` qo'shiladi (shu sotuv agenti).

> Eslatma: barcha pul/foiz maydonlari **double** (loyiha decimal → double ga o'tgan).

### 3.2 DbContext + Migration
- `WmsDbContext` ga `DbSet<Agent>`, `DbSet<CommissionRecord>` qo'shish.
- FK konfiguratsiyalar (Counterparty→Agent, Transfer→Agent, CommissionRecord→Agent/Transfer).
- Migration: `dotnet ef migrations add AddAgentModule`.

### 3.3 Permission seed (yangi)
WmsDbContext + migration'ga 2 ta yangi permission:
- `agents.view` — Agentlarni ko'rish
- `agents.manage` — Agentlarni boshqarish (CRUD, komissiya to'lovi)

Admin roliga avtomatik biriktiriladi (`EnsureAdminPermissionsAsync`).

### 3.4 Application qatlami
- DTO'lar: `AgentDto`, `AgentCreateDto`, `AgentUpdateDto`, `CommissionRecordDto`, `AgentSalesReportDto`, `PayCommissionDto`.
- Interfeys: `IAgentService`, `ICommissionService` (yoki bittada birlashtirish).

### 3.5 Service mantig'i
**`AgentService`** — CRUD (tenant-scoped, soft-delete).

**`CommissionService`:**
- `TransferService.ConfirmAsync` ichiga ulanish nuqtasi: agar `Transfer.Type == Outgoing && AgentId != null` →
  `CommissionRecord` yaratiladi (SaleAmount = items yig'indisi, percent = agent foizi yoki override, amount = sale × percent / 100).
- `GetAgentSalesReport(agentId, from, to)` — sotuvlar soni/summasi, jami komissiya, to'langan/qolgan.
- `PayCommission(agentId, amount, ...)` — komissiyani to'langan deb belgilash + ixtiyoriy Finance `Expense` yozuvi.

### 3.6 Controllers
**`AgentsController`** (`api/agents`):
```
GET    /api/agents                      - ro'yxat
POST   /api/agents                      - yaratish
PUT    /api/agents/{id}                 - tahrirlash
DELETE /api/agents/{id}                 - soft delete
GET    /api/agents/{id}/sales           - sotuv hisoboti (?from=&to=)
GET    /api/agents/{id}/commissions     - komissiya yozuvlari
POST   /api/agents/{id}/commissions/pay - komissiya to'lash
```
- Mavjud `TransfersController` / `CounterpartiesController` DTO'lariga `agentId` qo'shiladi.

### 3.7 DI (Program.cs)
- `IAgentService`, `ICommissionService` ro'yxatga olinadi.

---

## 4. FRONTEND (wms-ui)

### 4.1 Models
- `agent.model.ts`: `Agent`, `AgentCreateDto`, `CommissionRecord`, `AgentSalesReport`.
- `transfer.model.ts` va `counterparty.model.ts` ga `agentId` qo'shish.

### 4.2 Service
- `agent.service.ts`: CRUD + `getSales()` + `getCommissions()` + `payCommission()` (ApiService orqali).

### 4.3 Komponentlar / sahifalar
- `modules/agents/agent-list/` — ro'yxat + CRUD dialog (foiz, telefon, faol holat). Pattern: `settings/users` ga o'xshash.
- `modules/agents/agent-detail/` — agent kartasi: sotuvlar, komissiya hisoboti, to'lov tugmasi, ApexCharts (sotuv dinamikasi).
- **Transfer create** (`transfers/transfer-create`): "Agent orqali" toggle + agent dropdown + komissiya foizi (default to'ladi, override mumkin).
- **Transfer detail / list**: agent ustuni/badge ko'rsatish.
- **Counterparty create/edit**: agent biriktirish dropdown.

### 4.4 Routing + guard + sidebar
- `app.routes.ts`: `agents` yo'li, `permissionGuard('agents.view')` bilan.
- Sidebar'ga "Agentlar" menyusi (`pi-id-card` yoki `pi-users` ikon).
- `has-permission` direktivasi bilan tugmalar himoyalanadi.

### 4.5 i18n
4 ta tilga (uz, ru, en, uz-cyrl) `agent.*` kalitlar:
`agent.title, agent.name, agent.phone, agent.commission, agent.viaAgent,
agent.direct, agent.sales, agent.commissionDue, agent.commissionPaid, agent.pay`.

---

## 5. AGENT PORTALI (2-bosqich, ixtiyoriy)

Mavjud Counterparty portali patternidan nusxa:
- `Agent.PortalEnabled` + alohida JWT (`agentId` claim).
- `PortalController` ga agent endpointlari yoki yangi `AgentPortalController`.
- Front: `/portal` ichida agent login + dashboard (o'z sotuvlari, komissiyasi). Read-only.

---

## 6. BAJARISH TARTIBI (build order)

### Bosqich A — Backend asos
- [ ] `Agent`, `CommissionRecord` entity'lari
- [ ] `Counterparty.AgentId`, `Transfer.AgentId` qo'shish
- [ ] DbContext + `AddAgentModule` migration
- [ ] `agents.view/manage` permission seed
- [ ] DTO'lar + interfeyslar
- [ ] `AgentService` (CRUD)
- [ ] `CommissionService` + `TransferService.Confirm` ga ulash
- [ ] `AgentsController`
- [ ] DI ro'yxati
- [ ] Build: 0 xato

### Bosqich B — Frontend asos
- [ ] Modellar + `agent.service.ts`
- [ ] Agent ro'yxat + CRUD sahifasi
- [ ] Agent detail + komissiya/sotuv hisoboti
- [ ] Transfer create'ga "agent orqali" tanlovi
- [ ] Counterparty'ga agent biriktirish
- [ ] Route + guard + sidebar + i18n (4 til)
- [ ] Build: 0 xato

### Bosqich C — Test
- [ ] Agent yaratish → mijozga biriktirish
- [ ] Agent orqali sotuv → Confirmed → komissiya yozuvi yaratiladi
- [ ] Hisobotda komissiya to'g'ri ko'rinadi
- [ ] Komissiya to'lovi → qolgan summa kamayadi
- [ ] To'g'ridan-to'g'ri sotuv → komissiya **yaratilmaydi**

### Bosqich D — Portal (ixtiyoriy, keyin)
- [ ] Agent portal login + dashboard (read-only)

### Bosqich E — Deploy
- [ ] `dotnet publish` + `ng build` → serverga yuklash

---

## 7. TEGADIGAN MAVJUD FAYLLAR (ta'sir doirasi)

**Backend:** `WmsDbContext.cs`, `Counterparty.cs`, `Transfer.cs`, `TransferService.cs`
(Confirm mantig'i), `Program.cs`, Transfer/Counterparty DTO'lari, yangi migration.

**Frontend:** `app.routes.ts`, sidebar komponenti, `transfer-create`, `transfer-list`,
`transfer-detail`, counterparty create/edit, 4 ta i18n fayli, `permission.service`/seed.

---

## 8. OCHIQ SAVOLLAR (tasdiqlash kerak)

1. **D4/D5:** Komissiya sotuv summasidan (Confirmed'da) mi, yoki to'langan puldan (Payment'da) mi?
2. **D1:** Agent asosiy tizimga kiradimi (rol bilan), portal keyinmi? Yoki faqat portalmi?
3. **D2:** Mijoz doimiy agentga biriktiriladimi, yoki har sotuvda alohida tanlanadimi?
4. Komissiya foizi mahsulot/kategoriya bo'yicha har xil bo'lishi kerakmi (default: yo'q, yagona foiz)?
5. Delivery moduli shu navbatda kerakmi (default: yo'q)?
