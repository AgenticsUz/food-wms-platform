# Agent Module — Frontend Progress

Branch: `new-style` · Backend 100% tayyor · Frontend `ng build --configuration production` → 0 xato

## ✅ Prompt 1 — Agent ro'yxati + CRUD
- `modules/agents/agent-list/` (ts/html/scss) — TableModule, CRUD dialog (name, phone, commissionPercent, isActive, portalEnabled, portalPhone, portalPassword)
- `agents.view` / `agents.manage` ruxsatlar (`hasPermission`)
- `agents.routes.ts`, `app.routes.ts` (`/agents` + `permissionGuard('agents.view')`)
- Sidebar: `nav.agents` (`pi pi-id-card`)
- i18n: `nav.agents` + `agent` bloki (4 til)

## ✅ Prompt 2 — Agent detali
- `modules/agents/agent-detail/` (ts/html/scss)
- Xulosa kartalari (totalSales, salesCount, commissionConfirmed/pending/paid/due)
- Komissiya jadvali + status o'zgartirish (`agents.manage`)
- "Komissiya to'lash" dialogi (amount, note, recordAsExpense)
- ApexCharts timeline (sotuv + komissiya)
- Route `:id`, i18n kengaytirildi (4 til)

## ✅ Prompt 3 — Transfer integratsiyasi
- `transfer.model.ts`: `agentId`, `agentName`, `commissionPercent` (Transfer + CreateDto)
- `transfer-create`: "Agent orqali" toggle (faqat Outgoing), agent Select, foiz avtomatik to'ldirish, mijozning agentini oldindan tanlash
- `transfer-list` / `transfer-detail`: binafsha (purple) agent badge
- i18n: viaAgent, direct, selectAgent (4 til)

## ✅ Prompt 4 — Mijozga doimiy agent
- `counterparty.model.ts`: `agentId`, `agentName` (Counterparty + CreateDto)
- `client-list`: agent Select (form) + agentName ustuni
- `counterparty-detail`: agent badge
- i18n: assignAgent, noAgent (4 til)

## ✅ Prompt 5 — Agent portali (read-only)
- `core/services/agent-portal.service.ts` (alohida `agentPortalToken`)
- `core/guards/agent-portal.guard.ts`
- `auth.interceptor.ts`: `/agent-portal/` uchun alohida token branch
- `modules/agent-portal/`: login, layout, dashboard (read-only sotuv/komissiya)
- `app.routes.ts`: `/agent-portal/*`
- i18n: `agentPortal` bloki (4 til)

## ✅ Bugfix — Agent tizimga kira olmasligi (login)
**Muammo:** Konsolda `POST /api/auth/login` → 400 "Invalid credentials" va `/api/notifications/unread-count` → 401.
**Sabab:** Asosiy login (`/api/auth/login`) faqat tenant `Users` uchun. Agentlar `Users` emas — ular alohida JWT bilan `/agent-portal/login` orqali kiradi. Foydalanuvchi agent parolini asosiy login oynasiga kiritgan.
**Tekshirildi (backend to'g'ri):** `AgentPortalController` → `POST /api/agent-portal/login`; `AgentService.PortalLoginAsync` → `PhoneHelper.Normalize` telefonni moslaydi, BCrypt parol; DTO'lar frontend bilan mos.
**Tuzatish:** Asosiy login sahifasiga (`modules/auth/login/`) portal havolalari qo'shildi:
- `login.component.ts`: `RouterLink` import qilindi
- `login.component.html`: `.login-alt-links` — "Agent portali" (`/agent-portal/login`) + "Portal" (`/portal/login`) havolalari
- `login.component.scss`: `.login-alt-links` stillari
- Mavjud i18n kalitlar ishlatildi (`agentPortal.title`, `partners.portal`)

**Eslatma:** ApexCharts konsol xatosi (`Cannot read properties of undefined (reading 'hidden')`) agent modulidan emas — grafik mavjud ishlaydigan `finance-summary` bilan aynan bir xil pattern ishlatadi; oldindan mavjud (zoneless + ApexCharts) muammo.
**Agentlar dizayn bo'yicha asosiy tizimga kira olmaydi (faqat read-only portal).** Agar to'liq `User` kiritish kerak bo'lsa — alohida backend vazifasi.

## ✅ Return (Qaytarish) transfer turi
Backend tayyor edi (`TransferType.Return=5`, `ReturnReason`, `OriginalTransferId`).
- `transfer.model.ts`: `TransferType.Return = 5`; yangi `ReturnReason` enum (Expired=1/Unsold=2/Defective=3/Other=4); `Transfer` ga `returnReason`, `returnReasonName`, `originalTransferId`; `TransferCreateDto` ga `returnReason`, `originalTransferId`
- `transfer-create`: `typeOptions` ga "Qaytarish", `returnReasonOptions` computed (tilga reaktiv), `returnReason`/`originalTransferId` signallari, `isReturn` getter
  - Return UI: sabab Select + asl o'tkazma InputNumber (transfer id), mijoz (client filtri), maqsad ombori (ToWarehouseId)
  - Validatsiya: Return da CounterpartyId + ToWarehouseId + ReturnReason majburiy
  - Submit DTO ga faqat `isReturn` bo'lganda qo'shiladi
- `transfer-list`: Return uchun binafsha badge (`bg-purple-100 text-purple-800`, `pi-replay`), tur filtriga Return
- `transfer-detail`: `returnReasonName` ko'rsatish + `originalTransferId` → `/transfers/:id` routerLink
- i18n (4 til) `transfer` bloki: return, returnReason, expired, unsold, defective, other, originalTransfer, selectReturnReason

## Holat
Barcha ishlar `ng build --configuration production` → **0 xato** bilan tasdiqlangan. 4 til i18n JSON valid.

## Deploy
```
ng build --configuration production
dist/wms-ui/browser/ → /var/www/wms/ui/
systemctl reload nginx
```

## Keyingi chatda davom ettirish uchun eslatma
- Return "Asl o'tkazma" hozircha oddiy InputNumber (transfer id) — kelajakda mijozning oldingi Outgoing sotuvlari Select'iga aylantirish mumkin.
- Prompt 5 agent portali faqat dashboard (read-only). Kerak bo'lsa alohida sotuvlar/komissiya sahifalari qo'shilishi mumkin.
- Backend joylashuvi: `wms-api/` (masalan `WMS.API/Controllers/AgentsController.cs`, `AgentPortalController.cs`; `WMS.Infrastructure/Services/AgentService.cs`; DTO: `WMS.Application/DTOs/Agents/AgentDtos.cs`).
