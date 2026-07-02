using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Production;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class ProductionService : IProductionService
{
    private readonly WmsDbContext _db;
    private readonly INotificationService _notifications;
    public ProductionService(WmsDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    // === Stages ===
    public async Task<List<ProductionStageDto>> GetStagesAsync(int tenantId)
    {
        return await _db.ProductionStages.Where(s => s.TenantId == tenantId)
            .OrderBy(s => s.OrderNumber)
            .Select(s => new ProductionStageDto
            {
                Id = s.Id, Name = s.Name, OrderNumber = s.OrderNumber, Description = s.Description
            }).ToListAsync();
    }

    public async Task<ProductionStageDto> CreateStageAsync(int tenantId, CreateProductionStageDto dto)
    {
        var s = new ProductionStage
        {
            TenantId = tenantId, Name = dto.Name, OrderNumber = dto.OrderNumber, Description = dto.Description
        };
        _db.ProductionStages.Add(s);
        await _db.SaveChangesAsync();
        return new ProductionStageDto { Id = s.Id, Name = s.Name, OrderNumber = s.OrderNumber, Description = s.Description };
    }

    public async Task<ProductionStageDto> UpdateStageAsync(int tenantId, int id, UpdateProductionStageDto dto)
    {
        var s = await _db.ProductionStages.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Stage not found");
        s.Name = dto.Name; s.OrderNumber = dto.OrderNumber; s.Description = dto.Description;
        await _db.SaveChangesAsync();
        return new ProductionStageDto { Id = s.Id, Name = s.Name, OrderNumber = s.OrderNumber, Description = s.Description };
    }

    public async Task DeleteStageAsync(int tenantId, int id)
    {
        var s = await _db.ProductionStages.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Stage not found");
        s.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task ReorderStagesAsync(int tenantId, List<int> ids)
    {
        var stages = await _db.ProductionStages
            .Where(x => x.TenantId == tenantId && ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
        for (int i = 0; i < ids.Count; i++)
        {
            if (stages.TryGetValue(ids[i], out var s))
                s.OrderNumber = i + 1;
        }
        await _db.SaveChangesAsync();
    }

    // === Recipes ===
    public async Task<List<ProductionRecipeDto>> GetRecipesAsync(int tenantId)
    {
        return await _db.ProductionRecipes
            .Where(r => r.TenantId == tenantId)
            .Include(r => r.OutputProduct).Include(r => r.OutputUnit)
            .Select(r => new ProductionRecipeDto
            {
                Id = r.Id, Name = r.Name, OutputProductId = r.OutputProductId,
                OutputProductName = r.OutputProduct.Name, OutputQuantity = r.OutputQuantity,
                OutputUnitId = r.OutputUnitId, OutputUnitName = r.OutputUnit.Name, IsActive = r.IsActive
            }).ToListAsync();
    }

    public async Task<ProductionRecipeDto> GetRecipeByIdAsync(int tenantId, int id)
    {
        var r = await _db.ProductionRecipes
            .Include(r => r.OutputProduct).Include(r => r.OutputUnit)
            .Include(r => r.RecipeStages).ThenInclude(rs => rs.Stage)
            .Include(r => r.RecipeStages).ThenInclude(rs => rs.OutputProduct)
            .Include(r => r.RecipeStages).ThenInclude(rs => rs.Inputs).ThenInclude(i => i.Product)
            .Include(r => r.RecipeStages).ThenInclude(rs => rs.Inputs).ThenInclude(i => i.Unit)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId)
            ?? throw new NotFoundException("Recipe not found");

        return MapRecipeToDto(r);
    }

    private async Task ValidateRecipeReferencesAsync(int tenantId, CreateRecipeDto dto)
    {
        // Products: recipe output + per-stage outputs + per-input products
        var productIds = new List<int> { dto.OutputProductId };
        productIds.AddRange(dto.Stages
            .Where(s => s.OutputProductId.HasValue)
            .Select(s => s.OutputProductId!.Value));
        productIds.AddRange(dto.Stages.SelectMany(s => s.Inputs).Select(i => i.ProductId));
        var distinctProductIds = productIds.Distinct().ToList();
        var productCount = await _db.Products
            .CountAsync(p => p.TenantId == tenantId && distinctProductIds.Contains(p.Id));
        if (productCount != distinctProductIds.Count)
            throw new NotFoundException("One or more products not found");

        // Units: recipe output unit + per-input units
        var unitIds = new List<int> { dto.OutputUnitId };
        unitIds.AddRange(dto.Stages.SelectMany(s => s.Inputs).Select(i => i.UnitId));
        var distinctUnitIds = unitIds.Distinct().ToList();
        var unitCount = await _db.Units
            .CountAsync(u => u.TenantId == tenantId && distinctUnitIds.Contains(u.Id));
        if (unitCount != distinctUnitIds.Count)
            throw new NotFoundException("One or more units not found");

        // Production stages
        var stageIds = dto.Stages.Select(s => s.StageId).Distinct().ToList();
        if (stageIds.Count > 0)
        {
            var stageCount = await _db.ProductionStages
                .CountAsync(s => s.TenantId == tenantId && stageIds.Contains(s.Id));
            if (stageCount != stageIds.Count)
                throw new NotFoundException("One or more production stages not found");
        }

        // Warehouses (per-stage output warehouse)
        var warehouseIds = dto.Stages
            .Where(s => s.OutputWarehouseId.HasValue)
            .Select(s => s.OutputWarehouseId!.Value)
            .Distinct().ToList();
        if (warehouseIds.Count > 0)
        {
            var warehouseCount = await _db.Warehouses
                .CountAsync(w => w.TenantId == tenantId && warehouseIds.Contains(w.Id));
            if (warehouseCount != warehouseIds.Count)
                throw new NotFoundException("One or more warehouses not found");
        }
    }

    public async Task<ProductionRecipeDto> CreateRecipeAsync(int tenantId, CreateRecipeDto dto)
    {
        await ValidateRecipeReferencesAsync(tenantId, dto);

        var recipe = new ProductionRecipe
        {
            TenantId = tenantId, Name = dto.Name, OutputProductId = dto.OutputProductId,
            OutputQuantity = dto.OutputQuantity, OutputUnitId = dto.OutputUnitId
        };

        foreach (var stageDto in dto.Stages)
        {
            var recipeStage = new RecipeStage
            {
                StageId = stageDto.StageId, OrderNumber = stageDto.OrderNumber,
                OutputProductId = stageDto.OutputProductId,
                ExpectedOutputQty = stageDto.ExpectedOutputQty,
                AllowWarehouseOutput = stageDto.AllowWarehouseOutput,
                OutputWarehouseId = stageDto.OutputWarehouseId
            };
            foreach (var inputDto in stageDto.Inputs)
            {
                recipeStage.Inputs.Add(new RecipeStageItem
                {
                    ProductId = inputDto.ProductId, Quantity = inputDto.Quantity, UnitId = inputDto.UnitId
                });
            }
            recipe.RecipeStages.Add(recipeStage);
        }

        _db.ProductionRecipes.Add(recipe);
        await _db.SaveChangesAsync();
        return await GetRecipeByIdAsync(tenantId, recipe.Id);
    }

    public async Task<ProductionRecipeDto> UpdateRecipeAsync(int tenantId, int id, CreateRecipeDto dto)
    {
        await ValidateRecipeReferencesAsync(tenantId, dto);

        var recipe = await _db.ProductionRecipes
            .Include(r => r.RecipeStages).ThenInclude(rs => rs.Inputs)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId)
            ?? throw new NotFoundException("Recipe not found");

        recipe.Name = dto.Name; recipe.OutputProductId = dto.OutputProductId;
        recipe.OutputQuantity = dto.OutputQuantity; recipe.OutputUnitId = dto.OutputUnitId;

        // Remove old stages and inputs
        foreach (var stage in recipe.RecipeStages)
            _db.RecipeStageItems.RemoveRange(stage.Inputs);
        _db.RecipeStages.RemoveRange(recipe.RecipeStages);

        // Add new
        foreach (var stageDto in dto.Stages)
        {
            var recipeStage = new RecipeStage
            {
                RecipeId = id, StageId = stageDto.StageId, OrderNumber = stageDto.OrderNumber,
                OutputProductId = stageDto.OutputProductId,
                ExpectedOutputQty = stageDto.ExpectedOutputQty,
                AllowWarehouseOutput = stageDto.AllowWarehouseOutput,
                OutputWarehouseId = stageDto.OutputWarehouseId
            };
            foreach (var inputDto in stageDto.Inputs)
            {
                recipeStage.Inputs.Add(new RecipeStageItem
                {
                    ProductId = inputDto.ProductId, Quantity = inputDto.Quantity, UnitId = inputDto.UnitId
                });
            }
            _db.RecipeStages.Add(recipeStage);
        }

        await _db.SaveChangesAsync();
        return await GetRecipeByIdAsync(tenantId, id);
    }

    public async Task DeleteRecipeAsync(int tenantId, int id)
    {
        var r = await _db.ProductionRecipes.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new NotFoundException("Recipe not found");
        r.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    // === Orders ===
    public async Task<List<ProductionOrderDto>> GetOrdersAsync(int tenantId, ProductionOrderStatus? status)
    {
        var q = _db.ProductionOrders.Where(o => o.TenantId == tenantId)
            .Include(o => o.Recipe).ThenInclude(r => r.OutputProduct)
            .Include(o => o.AssignedToUser)
            .AsQueryable();
        if (status.HasValue) q = q.Where(o => o.Status == status.Value);

        return await q.OrderByDescending(o => o.CreatedAt)
            .Select(o => new ProductionOrderDto
            {
                Id = o.Id, RecipeId = o.RecipeId, RecipeName = o.Recipe.Name,
                OutputProductName = o.Recipe.OutputProduct.Name,
                PlannedQuantity = o.PlannedQuantity, Status = o.Status,
                PlannedStartDate = o.PlannedStartDate, PlannedEndDate = o.PlannedEndDate,
                AssignedToUserId = o.AssignedToUserId,
                AssignedToUserName = o.AssignedToUser != null ? o.AssignedToUser.FullName : null,
                Note = o.Note, CreatedAt = o.CreatedAt
            }).ToListAsync();
    }

    public async Task<ProductionOrderDto> GetOrderByIdAsync(int tenantId, int id)
    {
        var o = await _db.ProductionOrders
            .Include(o => o.Recipe).ThenInclude(r => r.OutputProduct)
            .Include(o => o.AssignedToUser)
            .Include(o => o.StageExecutions).ThenInclude(se => se.RecipeStage).ThenInclude(rs => rs.Stage)
            .Include(o => o.StageExecutions).ThenInclude(se => se.WorkerUser)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId)
            ?? throw new NotFoundException("Production order not found");

        return new ProductionOrderDto
        {
            Id = o.Id, RecipeId = o.RecipeId, RecipeName = o.Recipe.Name,
            OutputProductName = o.Recipe.OutputProduct.Name,
            PlannedQuantity = o.PlannedQuantity, Status = o.Status,
            PlannedStartDate = o.PlannedStartDate, PlannedEndDate = o.PlannedEndDate,
            AssignedToUserId = o.AssignedToUserId,
            AssignedToUserName = o.AssignedToUser?.FullName,
            Note = o.Note, CreatedAt = o.CreatedAt,
            StageExecutions = o.StageExecutions.OrderBy(se => se.RecipeStage.OrderNumber).Select(se => new StageExecutionDto
            {
                Id = se.Id, RecipeStageId = se.RecipeStageId,
                StageName = se.RecipeStage.Stage.Name,
                OrderNumber = se.RecipeStage.OrderNumber,
                PlannedQuantity = se.PlannedQuantity, ActualQuantity = se.ActualQuantity,
                WasteQuantity = se.WasteQuantity, ReworkQuantity = se.ReworkQuantity,
                WorkerUserId = se.WorkerUserId, WorkerUserName = se.WorkerUser?.FullName,
                StartTime = se.StartTime, EndTime = se.EndTime,
                Status = se.Status, Note = se.Note
            }).ToList()
        };
    }

    public async Task<ProductionOrderDto> CreateOrderAsync(int tenantId, CreateProductionOrderDto dto)
    {
        if (dto.PlannedQuantity <= 0)
            throw new AppException("Planned quantity must be greater than zero");

        var recipe = await _db.ProductionRecipes.Include(r => r.RecipeStages)
            .FirstOrDefaultAsync(r => r.Id == dto.RecipeId && r.TenantId == tenantId)
            ?? throw new NotFoundException("Recipe not found");

        if (dto.AssignedToUserId.HasValue &&
            !await _db.Users.AnyAsync(u => u.Id == dto.AssignedToUserId.Value && u.TenantId == tenantId))
            throw new NotFoundException("Assigned user not found");

        var order = new ProductionOrder
        {
            TenantId = tenantId, RecipeId = dto.RecipeId,
            PlannedQuantity = dto.PlannedQuantity,
            PlannedStartDate = dto.PlannedStartDate, PlannedEndDate = dto.PlannedEndDate,
            AssignedToUserId = dto.AssignedToUserId, Note = dto.Note
        };

        // Create stage executions from recipe stages
        foreach (var stage in recipe.RecipeStages.OrderBy(s => s.OrderNumber))
        {
            order.StageExecutions.Add(new StageExecution
            {
                RecipeStageId = stage.Id,
                PlannedQuantity = stage.ExpectedOutputQty ?? dto.PlannedQuantity
            });
        }

        _db.ProductionOrders.Add(order);
        await _db.SaveChangesAsync();
        return await GetOrderByIdAsync(tenantId, order.Id);
    }

    public async Task<ProductionOrderDto> StartOrderAsync(int tenantId, int id)
    {
        var order = await _db.ProductionOrders
            .Include(o => o.AssignedToUser)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId)
            ?? throw new NotFoundException("Order not found");
        if (order.Status != ProductionOrderStatus.Draft)
            throw new AppException("Only draft orders can be started");
        order.Status = ProductionOrderStatus.InProgress;
        await _db.SaveChangesAsync();

        var assignee = order.AssignedToUser?.FullName ?? "—";
        await _notifications.CreateAsync(tenantId, null,
            "Production Started",
            $"Buyurtma #{order.Id} boshlandi. Javobgar: {assignee}",
            NotificationType.ProductionStarted, "ProductionOrder", order.Id);

        return await GetOrderByIdAsync(tenantId, id);
    }

    public async Task<StageExecutionDto> ExecuteStageAsync(int tenantId, int orderId, int stageId, ExecuteStageDto dto)
    {
        if (dto.ActualQuantity < 0 || dto.WasteQuantity < 0 || dto.ReworkQuantity < 0)
            throw new AppException("Quantities cannot be negative");

        var order = await _db.ProductionOrders
            .Include(o => o.Recipe)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == tenantId)
            ?? throw new NotFoundException("Order not found");
        if (order.Status != ProductionOrderStatus.InProgress)
            throw new AppException("Order must be in progress");

        var execution = await _db.StageExecutions
            .Include(se => se.RecipeStage).ThenInclude(rs => rs.Stage)
            .Include(se => se.RecipeStage).ThenInclude(rs => rs.Inputs)
            .Include(se => se.WorkerUser)
            .FirstOrDefaultAsync(se => se.Id == stageId && se.ProductionOrderId == orderId)
            ?? throw new NotFoundException("Stage execution not found");

        if (dto.WorkerUserId.HasValue &&
            !await _db.Users.AnyAsync(u => u.Id == dto.WorkerUserId.Value && u.TenantId == tenantId))
            throw new NotFoundException("Worker not found");

        // Inputs are consumed only on the first completion; re-editing a completed
        // stage updates the recorded quantities without touching stock again.
        var firstCompletion = execution.Status != StageExecutionStatus.Completed;

        await using var tx = await _db.Database.BeginTransactionAsync();

        if (firstCompletion && execution.RecipeStage.Inputs.Count > 0)
        {
            // Recipe inputs are defined for one recipe batch (Recipe.OutputQuantity);
            // scale them to the order's planned quantity.
            var factor = order.Recipe.OutputQuantity > 0
                ? order.PlannedQuantity / order.Recipe.OutputQuantity
                : 1m;
            foreach (var input in execution.RecipeStage.Inputs)
                await DeductFromStockAsync(tenantId, input.ProductId, input.Quantity * factor);
        }

        execution.ActualQuantity = dto.ActualQuantity;
        execution.WasteQuantity = dto.WasteQuantity;
        execution.ReworkQuantity = dto.ReworkQuantity;
        execution.WorkerUserId = dto.WorkerUserId;
        execution.Note = dto.Note;
        execution.StartTime ??= DateTime.UtcNow;
        execution.EndTime = DateTime.UtcNow;
        execution.Status = StageExecutionStatus.Completed;

        // Intermediate output that is allowed to be stored in a warehouse.
        // The final stage's output is handled by CompleteOrderAsync (OutputProductId is
        // null there), so only explicit semi-finished outputs land here.
        if (firstCompletion && execution.RecipeStage.AllowWarehouseOutput
            && execution.RecipeStage.OutputWarehouseId.HasValue
            && execution.RecipeStage.OutputProductId.HasValue
            && dto.ActualQuantity > 0)
        {
            await AddProducedStockAsync(tenantId, execution.RecipeStage.OutputWarehouseId.Value,
                execution.RecipeStage.OutputProductId.Value, dto.ActualQuantity,
                $"SEMI-{order.Id}");
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new StageExecutionDto
        {
            Id = execution.Id, RecipeStageId = execution.RecipeStageId,
            StageName = execution.RecipeStage.Stage.Name,
            OrderNumber = execution.RecipeStage.OrderNumber,
            PlannedQuantity = execution.PlannedQuantity, ActualQuantity = execution.ActualQuantity,
            WasteQuantity = execution.WasteQuantity, ReworkQuantity = execution.ReworkQuantity,
            WorkerUserId = execution.WorkerUserId, WorkerUserName = execution.WorkerUser?.FullName,
            StartTime = execution.StartTime, EndTime = execution.EndTime,
            Status = execution.Status, Note = execution.Note
        };
    }

    public async Task<ProductionOrderDto> CompleteOrderAsync(int tenantId, int id)
    {
        var order = await _db.ProductionOrders
            .Include(o => o.Recipe).ThenInclude(r => r.OutputProduct)
            .Include(o => o.StageExecutions).ThenInclude(se => se.RecipeStage)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId)
            ?? throw new NotFoundException("Order not found");
        if (order.Status != ProductionOrderStatus.InProgress)
            throw new AppException("Order must be in progress");

        if (order.StageExecutions.Any(se =>
                se.Status != StageExecutionStatus.Completed && se.Status != StageExecutionStatus.Skipped))
            throw new AppException("All stages must be completed or skipped before completing the order");

        // The order's output is what the LAST completed stage produced — summing all
        // stages would count the same product once per stage.
        var finalStage = order.StageExecutions
            .Where(se => se.Status == StageExecutionStatus.Completed)
            .OrderByDescending(se => se.RecipeStage.OrderNumber)
            .FirstOrDefault();
        var totalOutput = finalStage?.ActualQuantity ?? 0;

        var finishedWarehouse = await _db.Warehouses
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.Type == WarehouseType.Finished)
            ?? throw new AppException("No finished goods warehouse configured for this tenant");

        await using var tx = await _db.Database.BeginTransactionAsync();

        order.Status = ProductionOrderStatus.Completed;

        if (totalOutput > 0)
        {
            var batch = await AddProducedStockAsync(tenantId, finishedWarehouse.Id,
                order.Recipe.OutputProductId, totalOutput, $"PROD-{order.Id}");

            // Auto-create ProductionOutput transfer for traceability
            var transfer = new Transfer
            {
                TenantId = tenantId, Type = TransferType.ProductionOutput,
                ToWarehouseId = finishedWarehouse.Id, Status = TransferStatus.Confirmed,
                ConfirmedAt = DateTime.UtcNow, Note = $"Production Order #{order.Id}"
            };
            transfer.Items.Add(new TransferItem
            {
                ProductId = order.Recipe.OutputProductId,
                Batch = batch, Quantity = totalOutput, UnitPrice = 0
            });
            _db.Transfers.Add(transfer);
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _notifications.CreateAsync(tenantId, null,
            "Production Completed",
            $"Buyurtma #{order.Id} bajarildi. {totalOutput:N0} dona {order.Recipe.OutputProduct.Name} tayyor omborga kiritildi",
            NotificationType.ProductionCompleted, "ProductionOrder", order.Id);

        return await GetOrderByIdAsync(tenantId, id);
    }

    /// FEFO-deducts the given quantity of a product from any of the tenant's
    /// warehouses (earliest expiry first). Reserved stock is not touched.
    private async Task DeductFromStockAsync(int tenantId, int productId, decimal quantity)
    {
        if (quantity <= 0) return;
        var remaining = quantity;

        var stocks = (await _db.WarehouseStocks
                .Include(s => s.Batch)
                .Where(s => s.TenantId == tenantId && s.ProductId == productId && s.Quantity > 0)
                .OrderBy(s => s.Batch.ExpiryDate ?? DateTime.MaxValue)
                .ToListAsync())
            .Where(s => s.Quantity - s.ReservedQuantity > 0)
            .ToList();

        foreach (var stock in stocks)
        {
            if (remaining <= 0) break;
            var take = Math.Min(remaining, stock.Quantity - stock.ReservedQuantity);
            stock.Quantity -= take;
            stock.Batch.RemainingQuantity -= take;
            remaining -= take;
        }

        if (remaining > 0)
        {
            var product = await _db.Products.FindAsync(productId);
            throw new AppException(
                $"Insufficient stock for input '{product?.Name ?? productId.ToString()}' (short by {remaining:N2})");
        }
    }

    /// Creates a new batch + stock row for produced goods in the given warehouse.
    private async Task<Batch> AddProducedStockAsync(int tenantId, int warehouseId,
        int productId, decimal quantity, string lotPrefix)
    {
        var location = await _db.Locations.FirstOrDefaultAsync(l => l.WarehouseId == warehouseId);
        if (location == null)
        {
            location = new Location { WarehouseId = warehouseId, Name = "Default", Code = "DEF" };
            _db.Locations.Add(location);
        }

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.TenantId == tenantId);
        var now = DateTime.UtcNow;
        var batch = new Batch
        {
            TenantId = tenantId, ProductId = productId,
            LotNumber = $"{lotPrefix}-{now:yyyyMMdd}-{Guid.NewGuid().ToString()[..6]}",
            ManufacturedDate = now,
            ExpiryDate = product?.ShelfLifeDays != null ? now.AddDays(product.ShelfLifeDays.Value) : null,
            InitialQuantity = quantity, RemainingQuantity = quantity
        };
        _db.Batches.Add(batch);

        _db.WarehouseStocks.Add(new WarehouseStock
        {
            TenantId = tenantId, WarehouseId = warehouseId,
            Location = location, ProductId = productId,
            Batch = batch, Quantity = quantity
        });

        return batch;
    }

    private static ProductionRecipeDto MapRecipeToDto(ProductionRecipe r) => new()
    {
        Id = r.Id, Name = r.Name, OutputProductId = r.OutputProductId,
        OutputProductName = r.OutputProduct.Name, OutputQuantity = r.OutputQuantity,
        OutputUnitId = r.OutputUnitId, OutputUnitName = r.OutputUnit.Name, IsActive = r.IsActive,
        Stages = r.RecipeStages.OrderBy(rs => rs.OrderNumber).Select(rs => new RecipeStageDto
        {
            Id = rs.Id, StageId = rs.StageId, StageName = rs.Stage.Name,
            OrderNumber = rs.OrderNumber, OutputProductId = rs.OutputProductId,
            OutputProductName = rs.OutputProduct?.Name,
            ExpectedOutputQty = rs.ExpectedOutputQty,
            AllowWarehouseOutput = rs.AllowWarehouseOutput,
            OutputWarehouseId = rs.OutputWarehouseId,
            Inputs = rs.Inputs.Select(i => new RecipeStageItemDto
            {
                Id = i.Id, ProductId = i.ProductId, ProductName = i.Product.Name,
                Quantity = i.Quantity, UnitId = i.UnitId, UnitName = i.Unit.Name
            }).ToList()
        }).ToList()
    };
}
