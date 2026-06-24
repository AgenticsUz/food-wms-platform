# Agent Moduli — Bajarilgan Ishlar (Progress)

> Yangilangan: 2026-06-24
> Asos: `AGENT_MODULE_PLAN.md`
> Holat: **Backend tayyor ✅ · Frontend jarayonda ⏳**

---

## 1. QABUL QILINGAN QARORLAR

| # | Savol | Qaror |
|---|---|---|
| Sotuv tanlovi | To'g'ridan-to'g'ri yoki agent orqali | Outgoing transferda `AgentId` ixtiyoriy. Tanlanmasa — to'g'ridan-to'g'ri, komissiya yo'q. |
| Komissiya hisoblanishi | Qachon yoziladi | Transfer **Confirmed** bo'lganda snapshot (foiz + summa saqlanadi). |
| Komissiya holati (universal) | Qarz / qaytarish holatlari | Har komissiyada holat: **Kutilmoqda** (mijoz to'lamagan), **Tasdiqlangan** (mijoz to'ladi), **Bekor** (mol qaytarildi). Bekor — hisobotdan chiqariladi. |
| Komissiya foizi | Manba | Har agent uchun yagona `CommissionPercent`; har sotuvda qo'lda override mumkin (`Transfer.CommissionPercent`). |
| Agent kabineti | Login/parol | Alohida JWT bilan agent portali (read-only): o'z sotuvlari, komissiya/foyda. |

---

## 2. BACKEND — TAYYOR ✅ (build: 0 xato, migration qo'llandi)

### Yangi entity'lar
- `WMS.Domain/Entities/Agent.cs` — Name, Phone, CommissionPercent, IsActive, Portal (Phone/PasswordHash/Enabled), ixtiyoriy UserId.
- `WMS.Domain/Entities/CommissionRecord.cs` — AgentId, TransferId, SaleAmount, CommissionPercent, CommissionAmount, **Status**, IsPaid, PaidAt (snapshot).
- `WMS.Domain/Enums/CommissionStatus.cs` — `Pending / Confirmed / Cancelled`.

### O'zgartirilgan entity'lar
- `Counterparty.cs` — `AgentId` (mijozning doimiy agenti).
- `Transfer.cs` — `AgentId` + `CommissionPercent?` (sotuv agenti va override foizi).

### Persistence
- `WmsDbContext.cs` — `DbSet<Agent>`, `DbSet<CommissionRecord>`; FK konfiguratsiyalar (Transfer→Agent, Counterparty→Agent, Agent→User, CommissionRecord→Agent/Transfer).
- Permission seed: `agents.view` (Id 23), `agents.manage` (Id 24) — Admin roliga `EnsureAdminPermissionsAsync` orqali avtomatik biriktiriladi.
- Migration: `20260624054624_AddAgentModule` — yaratildi va `database update` bilan qo'llandi.

### Application
- `DTOs/Agents/AgentDtos.cs` — AgentDto, CreateAgentDto, UpdateAgentDto, CommissionRecordDto, UpdateCommissionStatusDto, AgentSalesReportDto, AgentSalesPointDto, PayCommissionDto, AgentPortal* (login/profile/auth).
- `Interfaces/IAgentService.cs`.
- `DTOs/Transfers/TransferDtos.cs` — `AgentId`, `AgentName`, `CommissionPercent` (read + create).
- `DTOs/Counterparties/CounterpartyDtos.cs` — `AgentId`, `AgentName`.

### Infrastructure (servislar)
- `Services/AgentService.cs` — CRUD; sotuv hisoboti (`BuildReport`: jami sotuv, kutilayotgan/tasdiqlangan/bekor/to'langan komissiya, qarz, kunlik timeline); komissiya ro'yxati; `PayCommissionAsync` (eski qarzdan to'lash + ixtiyoriy Finance Expense); `UpdateCommissionStatusAsync`; agent portal login + ma'lumotlari.
- `Services/TransferService.cs` — `CreateAsync`/`MapToDto`/`GetTransferEntity`/`GetAllAsync` ga agent maydonlari; `ConfirmAsync` ichida `CreateCommission` — Outgoing + AgentId bo'lsa CommissionRecord yaratadi (dublikat himoyasi bilan).
- `Services/CounterpartyService.cs` — `AgentId`/`AgentName` create/update/list/map.

### API
- `Controllers/AgentsController.cs` (`api/agents`) — GET list, GET {id}, POST, PUT {id}, DELETE {id}, GET {id}/sales, GET {id}/commissions, POST {id}/commissions/pay, PUT commissions/{recordId}/status.
- `Controllers/AgentPortalController.cs` (`api/agent-portal`) — POST login, GET me, GET sales, GET commissions (alohida JWT: `agentId`, `tenantId`).
- `Program.cs` — `IAgentService` DI'ga qo'shildi.

---

## 3. FRONTEND — JARAYONDA ⏳

### Tayyor
- `core/models/agent.model.ts` — Agent, AgentCreateDto, CommissionStatus, CommissionRecord, AgentSalesReport, PayCommissionDto, AgentPortal* tiplar.
- `core/services/agent.service.ts` — CRUD + getSales + getCommissions + payCommission + updateCommissionStatus.

### Qolgan (rejada)
- [ ] `modules/agents/agent-list/` — ro'yxat + CRUD dialog (foiz, telefon, portal login/parol, faol holat).
- [ ] `modules/agents/agent-detail/` — sotuv hisoboti kartalari + komissiya jadvali (holat o'zgartirish) + to'lov tugmasi + ApexCharts (sotuv dinamikasi).
- [ ] `modules/agents/agents.routes.ts` + `app.routes.ts` ga `agents` yo'li (`permissionGuard('agents.view')`).
- [ ] Sidebar'ga "Agentlar" menyusi (`agents.view`).
- [ ] `transfer.model.ts` + `transfer-create` — "Agent orqali" toggle + agent dropdown + komissiya foizi (override).
- [ ] `transfer-list` / `transfer-detail` — agent ustuni/badge.
- [ ] `counterparty.model.ts` + client/supplier forma — agent biriktirish dropdown.
- [ ] **Agent kabineti (portal):** login + dashboard (o'z sotuvlari, komissiya: kutilayotgan/tasdiqlangan/to'langan/qolgan), alohida token, guard, interceptor yangilash, route.
- [ ] i18n — `agent.*` kalitlar (uz, ru, en, uz-cyrl).
- [ ] `ng build` — 0 xatoga keltirish.

---

## 4. TEKSHIRISH REJASI (Bosqich C)
- [ ] Agent yaratish → mijozga biriktirish.
- [ ] Agent orqali sotuv → Confirmed → komissiya yozuvi (Pending) yaratiladi.
- [ ] Mijoz to'ladi → holat Confirmed; mol qaytdi → holat Cancelled (hisobotdan chiqadi).
- [ ] Komissiya to'lovi → qolgan summa kamayadi, Finance Expense yoziladi.
- [ ] To'g'ridan-to'g'ri sotuv → komissiya **yaratilmaydi**.
- [ ] Agent o'z kabinetiga login → faqat o'z ma'lumotlarini ko'radi.
