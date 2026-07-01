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

## Deploy
```
ng build --configuration production
dist/wms-ui/browser/ → /var/www/wms/ui/
systemctl reload nginx
```
