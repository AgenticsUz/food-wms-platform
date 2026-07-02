# Return (Qaytarish) Transfer — Backend Progress

> **Holat:** Backend TO'LIQ tugallandi ✅ · Build 0 xato · Migration DB'ga qo'llandi
> **Sana:** 2026-07-01
> **Branch:** `new-style`

Mijozdan qaytgan mol omborga qaytadi, mijoz qarzi kamayadi, agent komissiyasi bekor bo'ladi.

---

## ✅ Bajarilgan ishlar (Backend — wms-api)

### 1. Enums (`WMS.Domain/Enums/`)
- **`TransferType.cs`** — `Return = 5` qo'shildi:
  ```csharp
  public enum TransferType { Incoming = 1, Outgoing = 2, Internal = 3, ProductionOutput = 4, Return = 5 }
  ```
- **`ReturnReason.cs`** — yangi enum yaratildi:
  ```csharp
  public enum ReturnReason { Expired = 1, Unsold = 2, Defective = 3, Other = 4 }
  ```

### 2. Entity (`WMS.Domain/Entities/Transfer.cs`)
Qo'shildi:
```csharp
public ReturnReason? ReturnReason { get; set; }   // set when Type == Return
public int? OriginalTransferId { get; set; }      // qaysi sotuv qaytarilyapti (ixtiyoriy)
```

### 3. DTOs (`WMS.Application/DTOs/Transfers/TransferDtos.cs`)
- **`TransferDto`** ga: `ReturnReason? ReturnReason`, `string? ReturnReasonName`, `int? OriginalTransferId`
- **`CreateTransferDto`** ga: `ReturnReason? ReturnReason`, `int? OriginalTransferId`

### 4. TransferService (`WMS.Infrastructure/Services/TransferService.cs`)
- **`CreateAsync`** — yangi Transfer ga `ReturnReason = dto.ReturnReason`, `OriginalTransferId = dto.OriginalTransferId` map qilindi.
- **`MapToDto`** — `ReturnReason`, `ReturnReasonName = t.ReturnReason?.ToString()`, `OriginalTransferId` to'ldirildi.
- **`ConfirmAsync`** switch ga qo'shildi:
  ```csharp
  case TransferType.Return:
      await ProcessReturn(transfer, tenantId);
      break;
  ```
- **`ProcessReturn(Transfer transfer, int tenantId)`** — yangi metod. `ProcessIncoming` uslubida:
  - `ToWarehouseId` majburiy (bo'lmasa exception).
  - Default location topiladi/yaratiladi.
  - Har item uchun yangi Batch (`LOT-RET-yyyyMMdd-{productId}-{guid6}` prefiksli — qaytarilgan mol ajralib tursin) + `WarehouseStock` QO'SHILADI.
- **`CancelCommissionForReturn(Transfer transfer, int tenantId)`** — yangi metod. `ConfirmAsync` ichida chaqiriladi:
  - `Type == Return` va `OriginalTransferId != null` bo'lsa,
  - `TenantId + TransferId == OriginalTransferId + Status != Cancelled` bo'lgan `CommissionRecord` topiladi,
  - `Status = CommissionStatus.Cancelled` qilinadi.
- **`UpdateDebt`** switch ga qo'shildi:
  ```csharp
  case TransferType.Return:
      debt.Amount -= totalPrice; // mijoz qaytardi — qarzi kamayadi
      break;
  ```
- **Notification** — `ConfirmAsync` da Return uchun alohida xabar:
  ```csharp
  await _notifications.CreateAsync(tenantId, null, "Return Received",
      $"Return #{transfer.Id} received from {transfer.Counterparty?.Name}. Amount: {totalAmount:N0}",
      NotificationType.Info, "Transfer", transfer.Id);
  ```
  (boshqa turlar uchun avvalgi "Transfer Confirmed" xabari saqlab qolindi)

### 5. Migration
- `dotnet ef migrations add AddReturnTransfer` → `20260701115640_AddReturnTransfer`
- `dotnet ef database update` — qo'llandi ✅
- Qo'shilgan ustunlar: `Transfers.ReturnReason` (INTEGER NULL), `Transfers.OriginalTransferId` (INTEGER NULL)

### 6. Build
- `dotnet build WMS.sln` → **0 xato, 0 warning** ✅

> Eslatma: Outgoing/Internal FEFO stock logikasiga tegilmadi. Return faqat stock QO'SHADI (Incoming kabi), qarzni KAMAYTIRADI, va `OriginalTransferId` bo'lsa komissiyani bekor qiladi.

---

## 🧪 Test rejasi (Swagger — hali qilinmagan)

1. Agent orqali **Outgoing** sotuv yarat (AgentId bilan) → **Confirm** → komissiya `Pending` yaratiladi.
2. **Return** transfer yarat:
   - `Type = Return (5)`
   - `OriginalTransferId` = o'sha sotuv Id'si
   - `ReturnReason = Expired (1)`
   - `ToWarehouseId` = ombor
   - `CounterpartyId` = o'sha mijoz
   - `Items` = qaytariladigan mahsulotlar
   → **Confirm**
3. Tekshir:
   - Stock ortdi ✅ (yangi `LOT-RET-...` batch bilan)
   - Mijoz qarzi kamaydi ✅
   - O'sha komissiya `Cancelled` bo'ldi ✅

---

## 📋 Keyingi qadam (Frontend — wms-ui, hali qilinmagan)

Return transfer turini UI ga qo'shish kerak:
- `core/models/transfer.model.ts` — `TransferType.Return`, `ReturnReason` enum, DTO maydonlari (`returnReason`, `returnReasonName`, `originalTransferId`).
- `transfer-create` — Return turini tanlash, `ReturnReason` dropdown, `OriginalTransferId` (sotuvni tanlash) forma.
- `transfer-list` / `transfer-detail` — Return badge/status, ReturnReason ko'rsatish.
- i18n (`en/ru/uz/uz-cyrl.json`) — Return, ReturnReason nomlari.

---

## O'zgargan/yaratilgan fayllar

| Fayl | O'zgarish |
|---|---|
| `WMS.Domain/Enums/TransferType.cs` | `Return = 5` |
| `WMS.Domain/Enums/ReturnReason.cs` | **yangi** |
| `WMS.Domain/Entities/Transfer.cs` | `ReturnReason?`, `OriginalTransferId?` |
| `WMS.Application/DTOs/Transfers/TransferDtos.cs` | DTO maydonlari |
| `WMS.Infrastructure/Services/TransferService.cs` | `ProcessReturn`, `CancelCommissionForReturn`, switch/map/notification |
| `WMS.Infrastructure/Migrations/20260701115640_AddReturnTransfer.*` | **yangi migration** |
