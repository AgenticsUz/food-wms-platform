using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class ProductionStage : TenantEntity
{
    public string Name { get; set; } = null!;
    public int OrderNumber { get; set; }
    public string? Description { get; set; }
}

public class ProductionRecipe : TenantEntity
{
    public Guid OutputProductId { get; set; }
    public Product OutputProduct { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal OutputQuantity { get; set; }
    public Guid OutputUnitId { get; set; }
    public Unit OutputUnit { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public ICollection<RecipeStage> RecipeStages { get; set; } = new List<RecipeStage>();
}

/// <summary>⚠️ F6 da <c>tenant_id</c> qo'shildi (D4).</summary>
public class RecipeStage : TenantEntity
{
    public Guid RecipeId { get; set; }
    public ProductionRecipe Recipe { get; set; } = null!;
    public Guid StageId { get; set; }
    public ProductionStage Stage { get; set; } = null!;
    public int OrderNumber { get; set; }
    public Guid? OutputProductId { get; set; }
    public Product? OutputProduct { get; set; }
    public decimal? ExpectedOutputQty { get; set; }
    public bool AllowWarehouseOutput { get; set; }
    public Guid? OutputWarehouseId { get; set; }
    public Warehouse? OutputWarehouse { get; set; }
    public ICollection<RecipeStageItem> Inputs { get; set; } = new List<RecipeStageItem>();
}

/// <summary>⚠️ F6 da <c>tenant_id</c> qo'shildi (D4).</summary>
public class RecipeStageItem : TenantEntity
{
    public Guid RecipeStageId { get; set; }
    public RecipeStage RecipeStage { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal Quantity { get; set; }
    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
}

public class ProductionOrder : TenantEntity
{
    public Guid RecipeId { get; set; }
    public ProductionRecipe Recipe { get; set; } = null!;
    public decimal PlannedQuantity { get; set; }

    /// <summary>Tenant ichidagi qisqa buyurtma raqami (<c>Transfer.Number</c> bilan bir xil naqsh).</summary>
    public int Number { get; set; }

    public ProductionOrderStatus Status { get; set; } = ProductionOrderStatus.Draft;
    public DateTime PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public UserProfile? AssignedToUser { get; set; }
    public string? Note { get; set; }
    public ICollection<StageExecution> StageExecutions { get; set; } = new List<StageExecution>();
}

/// <summary>⚠️ F6 da <c>tenant_id</c> qo'shildi (D4).</summary>
public class StageExecution : TenantEntity
{
    public Guid ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;
    public Guid RecipeStageId { get; set; }
    public RecipeStage RecipeStage { get; set; } = null!;
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal WasteQuantity { get; set; }
    public decimal ReworkQuantity { get; set; }
    public Guid? WorkerUserId { get; set; }
    public UserProfile? WorkerUser { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public StageExecutionStatus Status { get; set; } = StageExecutionStatus.Pending;
    public string? Note { get; set; }

    /// <summary>Shu bosqichda sarflangan xomashyoning QIYMATI (partiya tannarxlari bo'yicha).</summary>
    /// <remarks>
    /// Nega ustun kerak: xomashyo bosqich bajarilganda (alohida so'rovda) sarflanadi, tayyor
    /// mahsulot partiyasi esa buyurtma YAKUNLANGANDA tug'iladi — oradagi qiymat hech qayerda
    /// saqlanmasa, tayyor partiya tannarxsiz (<c>UnitCost = null</c>) qolar va foyda hisoboti
    /// uni «tannarxi noma'lum» deb ko'rsatardi. Taxminiy baho (o'rtacha qoldiq narxi) ATAYLAB
    /// ishlatilmaydi — u foydani jimgina buzardi.
    /// <see langword="null"/> — bosqich hali sarflamagan yoki tannarx noma'lum bo'lgan.
    /// </remarks>
    public decimal? MaterialCost { get; set; }

    public ICollection<QcCheck> QcChecks { get; set; } = new List<QcCheck>();
}

public class QcParameter : TenantEntity
{
    public string Name { get; set; } = null!;
    public string? Unit { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public QcParameterType ValueType { get; set; }
}

public class QcCheck : TenantEntity
{
    public Guid? StageExecutionId { get; set; }
    public StageExecution? StageExecution { get; set; }
    public Guid? TransferId { get; set; }
    public Transfer? Transfer { get; set; }
    public Guid ParameterId { get; set; }
    public QcParameter Parameter { get; set; } = null!;
    public string Value { get; set; } = null!;
    public bool IsPassed { get; set; }
    public Guid CheckedByUserId { get; set; }
    public UserProfile CheckedByUser { get; set; } = null!;
    public DateTime CheckedAt { get; set; }
    public string? Note { get; set; }
}
