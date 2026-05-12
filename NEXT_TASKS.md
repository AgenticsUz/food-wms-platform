# WMS Platform — Next Tasks

> Tasks prioritized based on client requests and business needs.
> Complete phases in order. Each phase = deployable increment.

---

## CURRENT STATUS ✅

All done:
- Bug fix (transfers, i18n, CRUD)
- Tailwind + UI improvements
- Analytics (ApexCharts professional)
- Permission system (backend + frontend)
- Excel export (6 pages with date filter)
- Excel import + templates (products, counterparties, users)
- PDF transfer document (QuestPDF)
- Notification bell (low stock, transfer confirmed/rejected)
- Dashboard reports + date range + monthly chart
- Demo data (seed)
- Barcode (USB + manual input)
- Currency widget (CBU API) + Finance calculator
- Phone normalize (+998XXXXXXXXX)
- Deploy (app.warehouse-system.uz + api.warehouse-system.uz)
- Badge colors fixed
- Login — tenant slug hidden

---

## PHASE 1 — This Week (Core improvements)

### 1.1 Return (Qaytarish) — Backend

Add Return transfer type for products returned from clients (expired or unsold).

New TransferType enum value:
- Return = 5

In TransferService:
- When type=Return: increase stock back to warehouse
- Link to original transfer (optional: OriginalTransferId field)
- Auto-create Finance record: reduce debt by return amount
- Auto-create Notification: "Return #ID received from {counterparty}"

New DTO fields:
- ReturnReason: enum (Expired, Unsold, Defective, Other)
- OriginalTransferId: int? (which transfer is being returned)

Build with zero errors.
Test: create Return transfer → stock increases → debt decreases.

---

### 1.2 Return — Frontend

In Transfer create page:
- Add "Qaytarish" option to transfer type selector
- When Return selected:
  - Show "Return Reason" dropdown (Muddati o'tgan / Sotilmagan / Brakli / Boshqa)
  - Show "Original Transfer" optional field
  - Counterparty = Client (who is returning)
  - Warehouse = destination (where stock goes back)

In Transfer list:
- Return type badge: bg-purple-100 text-purple-800
- Filter by Return type

In Transfer detail:
- Show return reason
- Show original transfer link if exists

Add transloco keys: transfer.return, transfer.returnReason,
transfer.expired, transfer.unsold, transfer.defective, transfer.other

Zero errors.

---

### 1.3 Permission Refresh on F5

Currently: permissions loaded only on login, F5 shows stale data.

Fix in AuthService:
```
refreshPermissions(): Observable<void> {
  return this.http.get('/auth/my-permissions').pipe(
    tap(res => {
      this.permissionService.setPermissions(res.data);
      localStorage.setItem('permissions', JSON.stringify(res.data));
    })
  );
}
```

In app.component.ts ngOnInit:
- If token exists → call refreshPermissions() silently
- This ensures F5 always loads fresh permissions from server

In authGuard:
- Call refreshPermissions() on first navigation after app load

Zero errors.
Test: change role permissions → F5 in other browser → changes visible immediately.

---

### 1.4 Notification improvements

Add new auto-notifications in backend:

1. Batch expiring soon (3 days before):
   - Run daily check (or on each request)
   - Type: Warning
   - Message: "{Product} partiyasi #{LotNumber} muddati {date} da tugaydi"
   - EntityType: "Batch", EntityId: batch.Id

2. Batch expired:
   - Type: Error
   - Message: "{Product} partiyasi #{LotNumber} muddati o'tdi!"

3. Production order completed:
   - Type: Info
   - Message: "Buyurtma #{Id} bajarildi. {Qty} dona {Product} tayyor omborga kiritildi"
   - EntityType: "ProductionOrder"

4. Production order started:
   - Type: Info
   - Message: "Buyurtma #{Id} boshlandi. Javobgar: {User}"

In NotificationBellService (frontend):
- Navigate to Batch detail when entityType === 'Batch'
- Navigate to Production order when entityType === 'ProductionOrder'

Build and test: create production order → complete it → notification appears.

---

### 1.5 Server deploy (new build)

After all Phase 1 changes:

Backend:
```
dotnet publish WMS.API -c Release -o ./publish
```
Upload publish/ → /var/www/wms/api/

Frontend:
```
ng build --configuration production
```
Upload dist/wms-ui/browser/ → /var/www/wms/ui/

Server:
```
systemctl restart wms-api
systemctl reload nginx
```

---

## PHASE 2 — Next Week (Client requests)

### 2.1 Audit Log — Backend

Client request: "История изменений — who did what, when"

Create AuditLog entity:
```
public class AuditLog : BaseEntity
{
    public int TenantId { get; set; }
    public int? UserId { get; set; }
    public string UserName { get; set; }
    public string Action { get; set; }      // Create, Update, Delete, Login
    public string EntityType { get; set; }  // Transfer, Product, User...
    public int? EntityId { get; set; }
    public string? OldValues { get; set; }  // JSON
    public string? NewValues { get; set; }  // JSON
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

Auto-log in these services:
- TransferService: Create, Confirm, Reject, Return
- ProductService: Create, Update, Delete
- UserService: Create, Update, Delete, Block
- AuthService: Login, Logout (failed login too)
- BatchService: Create, Update
- ProductionService: Start, Complete

New endpoints:
```
GET /api/audit-logs?entityType=&entityId=&userId=&fromDate=&toDate=
```
Paginated, filterable, tenant-scoped.

Add migration: dotnet ef migrations add AddAuditLogs

Build with zero errors.

---

### 2.2 Audit Log — Frontend

New page: Settings → Audit Log (/settings/audit-log)

Filters:
- Entity type dropdown (Transfer, Product, User...)
- User dropdown
- Date range (from/to)
- Action type (Create, Update, Delete, Login)

Table columns:
- Date/Time
- User
- Action (colored badge)
- Entity (Transfer #5, Product: Plombir...)
- Changes (show old→new values)

Row click → navigate to that entity

Permission: settings.audit (new permission, Admin only)

Add transloco keys: audit.title, audit.action, audit.entity,
audit.oldValue, audit.newValue, audit.ipAddress

Zero errors.

---

### 2.3 Batch Edit (Admin only)

Client request: "Partiyani o'zgartirsa/o'chirsa bo'ladimi?"

In BatchService:
- UpdateBatch: allow editing LotNumber, ExpiryDate, Notes (NOT quantity — dangerous)
- SoftDelete: IsDeleted=true, log in AuditLog

In Warehouse → Batches page:
- Edit button (admin only): *hasPermission="'warehouse.manage'"
  - Can edit: LotNumber, ExpiryDate, Notes
  - Cannot edit: Quantity (shown as readonly with warning)
- Delete button (admin only): confirm dialog with warning message

Warning message:
"Partiyani o'chirish ombor hisobiga ta'sir qiladi.
Faqat xato kiritilgan hollarda o'chiring."

Zero errors.

---

## PHASE 3 — Weekend (Platform)

### 3.1 Super Admin — Backend

New endpoints (no tenant scope, SuperAdmin role only):

```
GET    /api/admin/tenants              — list all tenants
POST   /api/admin/tenants             — create tenant
PUT    /api/admin/tenants/{id}        — update tenant
DELETE /api/admin/tenants/{id}        — soft delete

GET    /api/admin/tenants/{id}/modules — tenant modules
PUT    /api/admin/tenants/{id}/modules — toggle modules

GET    /api/admin/statistics          — platform stats
  {
    totalTenants, activeTenants,
    totalUsers, totalTransfers,
    totalProducts, revenueThisMonth
  }

POST   /api/auth/register             — onboarding (public)
  Creates: Tenant + Admin user + all modules enabled + demo data (optional)
  Returns: { tenantSlug, phone, tempPassword }
```

JWT: add SuperAdmin role claim
Policy: [Authorize(Policy = "SuperAdmin")]

Build with zero errors.

---

### 3.2 Super Admin — Frontend (separate Angular app)

New Angular app: platform-admin-ui/

Pages:

Public (no login):
- / → Landing (WMS features, pricing, demo link)
- /pricing → Plans (Basic/Pro/Enterprise)
- /register → Onboarding form (company name, phone, password)
  → POST /api/auth/register
  → Show credentials on success

Private (SuperAdmin login):
- /admin/dashboard → Platform statistics
- /admin/tenants → All tenants list (create, edit, modules, delete)
- /admin/tenants/{id} → Tenant detail + module toggles

Environment:
```
apiUrl: 'https://api.warehouse-system.uz/api'
```

SuperAdmin login: separate route /admin/login
JWT stored separately from tenant JWT

Zero errors.

---

## PHASE 4 — Later (Growth features)

### 4.1 Telegram Bot

When: after first real client starts using the system.

Notifications via Telegram:
- Low stock alert
- Transfer confirmed/rejected
- Batch expiring
- Production order completed
- Daily summary (optional)

Setup:
- @BotFather → get token
- User links their Telegram: Settings → Profile → "Connect Telegram"
- User sends /start to bot → gets chat_id
- Admin enters chat_id in user settings

Backend: TelegramService with SendMessage(chatId, message)
Trigger: alongside existing NotificationService.CreateAsync()

---

### 4.2 Agent & Delivery modules

When: after 2-3 real clients, based on their specific requests.

Agent module:
- Agent entity (linked to User)
- Commission % per agent
- Agent sales report
- Client portal for agents

Delivery module:
- Vehicle + Driver list
- Delivery route (one vehicle, N clients)
- Delivery order PDF (yuk xati)
- Driver confirms delivery via portal

---

### 4.3 AI Advisor

When: 10+ clients, stable data history.

Features:
- Low stock prediction based on sales history
- Production planning suggestions
- Waste reduction recommendations

Stack: Claude API (claude-sonnet-4-6)
Data: analytics endpoints already exist

---

## NOTES

- After every phase: ng build → 0 errors, deploy to server
- Audit log — read only, no edit/delete
- SuperAdmin sees all tenants, Tenant Admin sees only own data
- Telegram bot — optional per user, not forced
- AI Advisor — Phase 4, not before real data exists
