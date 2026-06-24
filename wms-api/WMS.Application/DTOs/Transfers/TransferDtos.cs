using WMS.Domain.Enums;

namespace WMS.Application.DTOs.Transfers;

public class TransferDto
{
    public int Id { get; set; }
    public TransferType Type { get; set; }
    public TransferStatus Status { get; set; }
    public int? FromWarehouseId { get; set; }
    public string? FromWarehouseName { get; set; }
    public int? ToWarehouseId { get; set; }
    public string? ToWarehouseName { get; set; }
    public int? CounterpartyId { get; set; }
    public string? CounterpartyName { get; set; }
    public int? AgentId { get; set; }
    public string? AgentName { get; set; }
    public decimal? CommissionPercent { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public string? Note { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TransferItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

public class TransferItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string UnitShortName { get; set; } = null!;
    public int? BatchId { get; set; }
    public string? LotNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CreateTransferDto
{
    public TransferType Type { get; set; }
    public int? FromWarehouseId { get; set; }
    public int? ToWarehouseId { get; set; }
    public int? CounterpartyId { get; set; }
    public int? AgentId { get; set; }
    public decimal? CommissionPercent { get; set; }
    public string? Note { get; set; }
    public List<CreateTransferItemDto> Items { get; set; } = new();
}

public class CreateTransferItemDto
{
    public int ProductId { get; set; }
    public int? BatchId { get; set; }
    public int? LocationId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
