using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Transfers;

public class TransferDto
{
    public Guid Id { get; set; }
    public TransferType Type { get; set; }
    public TransferStatus Status { get; set; }
    public Guid? FromWarehouseId { get; set; }
    public string? FromWarehouseName { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public string? ToWarehouseName { get; set; }
    public Guid? CounterpartyId { get; set; }
    public string? CounterpartyName { get; set; }
    public Guid? AgentId { get; set; }
    public string? AgentName { get; set; }
    public decimal? CommissionPercent { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public ReturnReason? ReturnReason { get; set; }
    public string? ReturnReasonName { get; set; }
    public Guid? OriginalTransferId { get; set; }
    public string? Note { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TransferItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

public class TransferItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string UnitShortName { get; set; } = null!;
    public Guid? BatchId { get; set; }
    public string? LotNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreateTransferDto
{
    public TransferType Type { get; set; }
    public Guid? FromWarehouseId { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public Guid? CounterpartyId { get; set; }
    public Guid? AgentId { get; set; }
    public decimal? CommissionPercent { get; set; }
    public ReturnReason? ReturnReason { get; set; }
    public Guid? OriginalTransferId { get; set; }
    public string? Note { get; set; }
    public List<CreateTransferItemDto> Items { get; set; } = new();
}

public class CreateTransferItemDto
{
    public Guid ProductId { get; set; }
    public Guid? BatchId { get; set; }
    public Guid? LocationId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
