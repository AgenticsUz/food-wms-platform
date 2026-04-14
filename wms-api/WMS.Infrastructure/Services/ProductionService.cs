using Microsoft.EntityFrameworkCore;
using WMS.Application.DTOs.Production;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

public class ProductionService : IProductionService
{
    private readonly WmsDbContext _db;
    public ProductionService(WmsDbContext db) => _db = db;

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
            ?? throw new Exception("Stage not found");
        s.Name = dto.Name; s.OrderNumber = dto.OrderNumber; s.Description = dto.Description;
        await _db.SaveChangesAsync();
        return new ProductionStageDto { Id = s.Id, Name = s.Name, OrderNumber = s.OrderNumber, Description = s.Description };
    }

    public async Task DeleteStageAsync(int tenantId, int id)
    {
        var s = await _db.ProductionStages.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId)
            ?? throw new Exception("Stage not found");
        s.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task ReorderStagesAsync(int tenantId, List<int> ids)
    {
        for (int i = 0; i < ids.Count; i++)
        {
            var s = await _db.ProductionStages.FirstOrDefaultAsync(x => x.Id == ids[i] && x.TenantId == tenantId);
            if (s != null) s.OrderNumber = i + 1;
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
            ?? throw new Exception("Recipe not found");

        return MapRecipeToDto(r);
    }

    public async Task<ProductionRecipeDto> CreateRecipeAsync(int tenantId, CreateRecipeDto dto)
    {
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
        var recipe = await _db.ProductionRecipes
            .Include(r => r.RecipeStages).ThenInclude(rs => rs.Inputs)
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId)
            ?? throw new Exception("Recipe not found");

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
            ?? throw new Exception("Recipe not found");
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
            ?? throw new Exception("Production order not found");

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
        var recipe = await _db.ProductionRecipes.Include(r => r.RecipeStages)
            .FirstOrDefaultAsync(r => r.Id == dto.RecipeId && r.TenantId == tenantId)
            ?? throw new Exception("Recipe not found");

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
        var order = await _db.ProductionOrders.FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId)
            ?? throw new Exception("Order not found");
        if (order.Status != ProductionOrderStatus.Draft)
            throw new Exception("Only draft orders can be started");
        order.Status = ProductionOrderStatus.InProgress;
        await _db.SaveChangesAsync();
        return await GetOrderByIdAsync(tenantId, id);
    }

    public async Task<StageExecutionDto> ExecuteStageAsync(int tenantId, int orderId, int stageId, ExecuteStageDto dto)
    {
        var order = await _db.ProductionOrders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == tenantId)
            ?? throw new Exception("Order not found");
        if (order.Status != ProductionOrderStatus.InProgress)
            throw new Exception("Order must be in progress");

        var execution = await _db.StageExecutions
            .Include(se => se.RecipeStage).ThenInclude(rs => rs.Stage)
            .Include(se => se.WorkerUser)
            .FirstOrDefaultAsync(se => se.Id == stageId && se.ProductionOrderId == orderId)
            ?? throw new Exception("Stage execution not found");

        execution.ActualQuantity = dto.ActualQuantity;
        execution.WasteQuantity = dto.WasteQuantity;
        execution.ReworkQuantity = dto.ReworkQuantity;
        execution.WorkerUserId = dto.WorkerUserId;
        execution.Note = dto.Note;
        execution.StartTime ??= DateTime.UtcNow;
        execution.EndTime = DateTime.UtcNow;
        execution.Status = StageExecutionStatus.Completed;

        await _db.SaveChangesAsync();

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
            .Include(o => o.StageExecutions)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId)
            ?? throw new Exception("Order not found");
        if (order.Status != ProductionOrderStatus.InProgress)
            throw new Exception("Order must be in progress");

        order.Status = ProductionOrderStatus.Completed;

        // Create production output transfer to finished goods warehouse
        var finishedWarehouse = await _db.Warehouses
            .FirstOrDefaultAsync(w => w.TenantId == tenantId && w.Type == WarehouseType.Finished);

        if (finishedWarehouse != null)
        {
            var totalOutput = order.StageExecutions
                .Where(se => se.Status == StageExecutionStatus.Completed)
                .Sum(se => se.ActualQuantity);

            var location = await _db.Locations.FirstOrDefaultAsync(l => l.WarehouseId == finishedWarehouse.Id);
            if (location != null && totalOutput > 0)
            {
                var batch = new Batch
                {
                    TenantId = tenantId, ProductId = order.Recipe.OutputProductId,
                    LotNumber = $"PROD-{DateTime.UtcNow:yyyyMMdd}-{order.Id}",
                    ManufacturedDate = DateTime.UtcNow,
                    ExpiryDate = order.Recipe.OutputProduct.ShelfLifeDays.HasValue
                        ? DateTime.UtcNow.AddDays(order.Recipe.OutputProduct.ShelfLifeDays.Value) : null,
                    InitialQuantity = totalOutput, RemainingQuantity = totalOutput
                };
                _db.Batches.Add(batch);
                await _db.SaveChangesAsync();

                // Auto-create ProductionOutput transfer
                var transfer = new Transfer
                {
                    TenantId = tenantId, Type = TransferType.ProductionOutput,
                    ToWarehouseId = finishedWarehouse.Id, Status = TransferStatus.Confirmed,
                    ConfirmedAt = DateTime.UtcNow, Note = $"Production Order #{order.Id}"
                };
                transfer.Items.Add(new TransferItem
                {
                    ProductId = order.Recipe.OutputProductId,
                    BatchId = batch.Id, Quantity = totalOutput, UnitPrice = 0
                });
                _db.Transfers.Add(transfer);

                _db.WarehouseStocks.Add(new WarehouseStock
                {
                    TenantId = tenantId, WarehouseId = finishedWarehouse.Id,
                    LocationId = location.Id, ProductId = order.Recipe.OutputProductId,
                    BatchId = batch.Id, Quantity = totalOutput
                });
            }
        }

        await _db.SaveChangesAsync();
        return await GetOrderByIdAsync(tenantId, id);
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
