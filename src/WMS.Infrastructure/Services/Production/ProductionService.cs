using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Production;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services;

// F6: tenant parametri yo'q (D4) — WmsDbContext filtri va RLS har so'rovni joriy tenantga
// cheklaydi, yangi yozuvga (bola jadvallar ham: RecipeStage, RecipeStageItem, StageExecution)
// TenantId'ni StampEntries qo'yadi.
public class ProductionService : IProductionService
{
    private readonly WmsDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IStockAllocator _stock;
    public ProductionService(WmsDbContext db, INotificationService notifications, IStockAllocator stock)
    {
        _db = db;
        _notifications = notifications;
        _stock = stock;
    }

    // === Stages ===
    public async Task<List<ProductionStageDto>> GetStagesAsync()
    {
        return await _db.ProductionStages
            .OrderBy(s => s.OrderNumber)
            .Select(s => new ProductionStageDto
            {
                Id = s.Id, Name = s.Name, OrderNumber = s.OrderNumber, Description = s.Description
            }).ToListAsync();
    }

    public async Task<ProductionStageDto> CreateStageAsync(CreateProductionStageDto dto)
    {
        var s = new ProductionStage
        {
            Name = dto.Name, OrderNumber = dto.OrderNumber, Description = dto.Description
        };
        _db.ProductionStages.Add(s);
        await _db.SaveChangesAsync();
        return new ProductionStageDto { Id = s.Id, Name = s.Name, OrderNumber = s.OrderNumber, Description = s.Description };
    }

    public async Task<ProductionStageDto> UpdateStageAsync(Guid id, UpdateProductionStageDto dto)
    {
        var s = await _db.ProductionStages.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Stage not found");
        s.Name = dto.Name; s.OrderNumber = dto.OrderNumber; s.Description = dto.Description;
        await _db.SaveChangesAsync();
        return new ProductionStageDto { Id = s.Id, Name = s.Name, OrderNumber = s.OrderNumber, Description = s.Description };
    }

    public async Task DeleteStageAsync(Guid id)
    {
        var s = await _db.ProductionStages.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Stage not found");
        s.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task ReorderStagesAsync(List<Guid> ids)
    {
        var stages = await _db.ProductionStages
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
        for (int i = 0; i < ids.Count; i++)
        {
            if (stages.TryGetValue(ids[i], out var s))
                s.OrderNumber = i + 1;
        }
        await _db.SaveChangesAsync();
    }

    // === Recipes ===
    public async Task<List<ProductionRecipeDto>> GetRecipesAsync()
    {
        return await _db.ProductionRecipes
            .Select(r => new ProductionRecipeDto
            {
                Id = r.Id, Name = r.Name, OutputProductId = r.OutputProductId,
                OutputProductName = r.OutputProduct.Name, OutputQuantity = r.OutputQuantity,
                OutputUnitId = r.OutputUnitId, OutputUnitName = r.OutputUnit.Name, IsActive = r.IsActive
            }).ToListAsync();
    }

    public async Task<ProductionRecipeDto> GetRecipeByIdAsync(Guid id)
    {
        var r = await _db.ProductionRecipes
            .Include(r => r.OutputProduct).Include(r => r.OutputUnit)
            .Include(r => r.RecipeStages).ThenInclude(rs => rs.Stage)
            .Include(r => r.RecipeStages).ThenInclude(rs => rs.OutputProduct)
            .Include(r => r.RecipeStages).ThenInclude(rs => rs.Inputs).ThenInclude(i => i.Product)
            .Include(r => r.RecipeStages).ThenInclude(rs => rs.Inputs).ThenInclude(i => i.Unit)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException("Recipe not found");

        return MapRecipeToDto(r);
    }

    private async Task ValidateRecipeReferencesAsync(CreateRecipeDto dto)
    {
        // Products: recipe output + per-stage outputs + per-input products
        var productIds = new List<Guid> { dto.OutputProductId };
        productIds.AddRange(dto.Stages
            .Where(s => s.OutputProductId.HasValue)
            .Select(s => s.OutputProductId!.Value));
        productIds.AddRange(dto.Stages.SelectMany(s => s.Inputs).Select(i => i.ProductId));
        var distinctProductIds = productIds.Distinct().ToList();
        var productCount = await _db.Products.CountAsync(p => distinctProductIds.Contains(p.Id));
        if (productCount != distinctProductIds.Count)
            throw new NotFoundException("One or more products not found");

        // Units: recipe output unit + per-input units
        var unitIds = new List<Guid> { dto.OutputUnitId };
        unitIds.AddRange(dto.Stages.SelectMany(s => s.Inputs).Select(i => i.UnitId));
        var distinctUnitIds = unitIds.Distinct().ToList();
        var unitCount = await _db.Units.CountAsync(u => distinctUnitIds.Contains(u.Id));
        if (unitCount != distinctUnitIds.Count)
            throw new NotFoundException("One or more units not found");

        // Production stages
        var stageIds = dto.Stages.Select(s => s.StageId).Distinct().ToList();
        if (stageIds.Count > 0)
        {
            var stageCount = await _db.ProductionStages.CountAsync(s => stageIds.Contains(s.Id));
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
            var warehouseCount = await _db.Warehouses.CountAsync(w => warehouseIds.Contains(w.Id));
            if (warehouseCount != warehouseIds.Count)
                throw new NotFoundException("One or more warehouses not found");
        }
    }

    public async Task<ProductionRecipeDto> CreateRecipeAsync(CreateRecipeDto dto)
    {
        await ValidateRecipeReferencesAsync(dto);

        var recipe = new ProductionRecipe
        {
            Name = dto.Name, OutputProductId = dto.OutputProductId,
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
        return await GetRecipeByIdAsync(recipe.Id);
    }

    public async Task<ProductionRecipeDto> UpdateRecipeAsync(Guid id, CreateRecipeDto dto)
    {
        await ValidateRecipeReferencesAsync(dto);

        var recipe = await _db.ProductionRecipes
            .Include(r => r.RecipeStages).ThenInclude(rs => rs.Inputs)
            .FirstOrDefaultAsync(r => r.Id == id)
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
        return await GetRecipeByIdAsync(id);
    }

    public async Task DeleteRecipeAsync(Guid id)
    {
        var r = await _db.ProductionRecipes.FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException("Recipe not found");
        r.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    // === Orders ===
    public async Task<List<ProductionOrderDto>> GetOrdersAsync(ProductionOrderStatus? status)
    {
        var q = _db.ProductionOrders.AsQueryable();
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

    public async Task<ProductionOrderDto> GetOrderByIdAsync(Guid id)
    {
        var o = await _db.ProductionOrders
            .Include(o => o.Recipe).ThenInclude(r => r.OutputProduct)
            .Include(o => o.AssignedToUser)
            .Include(o => o.StageExecutions).ThenInclude(se => se.RecipeStage).ThenInclude(rs => rs.Stage)
            .Include(o => o.StageExecutions).ThenInclude(se => se.WorkerUser)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id)
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

    public async Task<ProductionOrderDto> CreateOrderAsync(CreateProductionOrderDto dto)
    {
        if (dto.PlannedQuantity <= 0)
            throw new AppException("Planned quantity must be greater than zero");

        var recipe = await _db.ProductionRecipes.Include(r => r.RecipeStages)
            .FirstOrDefaultAsync(r => r.Id == dto.RecipeId)
            ?? throw new NotFoundException("Recipe not found");

        // *UserId — user_profile.id (Identity sub emas); profil tenant filtri ostida.
        if (dto.AssignedToUserId.HasValue &&
            !await _db.UserProfiles.AnyAsync(u => u.Id == dto.AssignedToUserId.Value))
            throw new NotFoundException("Assigned user not found");

        var order = new ProductionOrder
        {
            RecipeId = dto.RecipeId,
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
        return await GetOrderByIdAsync(order.Id);
    }

    public async Task<ProductionOrderDto> StartOrderAsync(Guid id)
    {
        var order = await _db.ProductionOrders
            .Include(o => o.AssignedToUser)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new NotFoundException("Order not found");
        if (order.Status != ProductionOrderStatus.Draft)
            throw new AppException("Only draft orders can be started");
        // ProductionOrder xmin bilan qo'riqlanadi (D13): parallel ikki «boshlash»dan ikkinchisi 409.
        order.Status = ProductionOrderStatus.InProgress;
        await _db.SaveChangesAsync();

        var assignee = order.AssignedToUser?.FullName ?? "—";
        await _notifications.CreateAsync(null,
            "Production Started",
            $"Buyurtma #{order.Id} boshlandi. Javobgar: {assignee}",
            NotificationType.ProductionStarted, "ProductionOrder", order.Id);

        return await GetOrderByIdAsync(id);
    }

    public async Task<StageExecutionDto> ExecuteStageAsync(Guid orderId, Guid stageId, ExecuteStageDto dto)
    {
        if (dto.ActualQuantity < 0 || dto.WasteQuantity < 0 || dto.ReworkQuantity < 0)
            throw new AppException("Quantities cannot be negative");

        var order = await _db.ProductionOrders
            .Include(o => o.Recipe)
            .FirstOrDefaultAsync(o => o.Id == orderId)
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
            !await _db.UserProfiles.AnyAsync(u => u.Id == dto.WorkerUserId.Value))
            throw new NotFoundException("Worker not found");

        // Inputs are consumed only on the first completion; re-editing a completed
        // stage updates the recorded quantities without touching stock again.
        //
        // F6 (D13): bir bosqichni parallel ikki marta yakunlash — ikkalasi ham «Pending» ni
        // o'qiydi va xomashyoni ikki marta yechardi. Endi StageExecution (hamda WarehouseStock
        // va Batch) xmin bilan qo'riqlanadi: ikkinchisining UPDATE'i 0 qator oladi →
        // DbUpdateConcurrencyException → 409, va barcha zaxira o'zgarishi shu BITTA
        // SaveChangesAsync ichida bo'lgani uchun hech narsa yarim qolmaydi.
        var firstCompletion = execution.Status != StageExecutionStatus.Completed;

        await using var tx = await _db.Database.BeginTransactionAsync();

        if (firstCompletion && execution.RecipeStage.Inputs.Count > 0)
        {
            // Recipe inputs are defined for one recipe batch (Recipe.OutputQuantity);
            // scale them to the order's planned quantity.
            var factor = order.Recipe.OutputQuantity > 0
                ? order.PlannedQuantity / order.Recipe.OutputQuantity
                : 1m;
            // Miqdor ustuni numeric(18,3): xotiradagi qiymat bazadagidan farq qilmasin
            // (aks holda partiya qoldig'i va zaxira qatori turlicha yaxlitlanardi).
            foreach (var input in execution.RecipeStage.Inputs)
                await DeductFromStockAsync(input.ProductId, Math.Round(input.Quantity * factor, 3));
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
            await AddProducedStockAsync(execution.RecipeStage.OutputWarehouseId.Value,
                execution.RecipeStage.OutputProductId.Value, dto.ActualQuantity,
                $"SEMI-{order.Id}");
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        // Ishchi o'zgargan bo'lsa navigatsiya eski profilni ko'rsatmasin.
        if (execution.WorkerUserId.HasValue && execution.WorkerUser?.Id != execution.WorkerUserId)
            await _db.Entry(execution).Reference(se => se.WorkerUser).LoadAsync();

        return new StageExecutionDto
        {
            Id = execution.Id, RecipeStageId = execution.RecipeStageId,
            StageName = execution.RecipeStage.Stage.Name,
            OrderNumber = execution.RecipeStage.OrderNumber,
            PlannedQuantity = execution.PlannedQuantity, ActualQuantity = execution.ActualQuantity,
            WasteQuantity = execution.WasteQuantity, ReworkQuantity = execution.ReworkQuantity,
            WorkerUserId = execution.WorkerUserId,
            WorkerUserName = execution.WorkerUserId.HasValue ? execution.WorkerUser?.FullName : null,
            StartTime = execution.StartTime, EndTime = execution.EndTime,
            Status = execution.Status, Note = execution.Note
        };
    }

    public async Task<ProductionOrderDto> CompleteOrderAsync(Guid id)
    {
        var order = await _db.ProductionOrders
            .Include(o => o.Recipe).ThenInclude(r => r.OutputProduct)
            .Include(o => o.StageExecutions).ThenInclude(se => se.RecipeStage)
            .FirstOrDefaultAsync(o => o.Id == id)
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
            .FirstOrDefaultAsync(w => w.Type == WarehouseType.Finished)
            ?? throw new AppException("No finished goods warehouse configured for this tenant");

        await using var tx = await _db.Database.BeginTransactionAsync();

        // ProductionOrder xmin bilan qo'riqlanadi (D13): parallel ikki yakunlashdan ikkinchisi
        // 409 oladi va tayyor mahsulot omborga ikki marta kirmaydi.
        order.Status = ProductionOrderStatus.Completed;

        if (totalOutput > 0)
        {
            var batch = await AddProducedStockAsync(finishedWarehouse.Id,
                order.Recipe.OutputProductId, totalOutput, $"PROD-{order.Id}");

            // Auto-create ProductionOutput transfer for traceability.
            // F6: ITransferService orqali EMAS — u ProductionOutput turini foydalanuvchi
            // so'rovidan qabul qilmaydi (tizim yaratadi) va o'z SaveChanges'i bilan zaxira
            // kirimini buyurtma holatidan ajratib yuborardi. Shu yerda — bitta ish birligi.
            var transfer = new Transfer
            {
                Type = TransferType.ProductionOutput,
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

        await _notifications.CreateAsync(null,
            "Production Completed",
            $"Buyurtma #{order.Id} bajarildi. {totalOutput:N0} dona {order.Recipe.OutputProduct.Name} tayyor omborga kiritildi",
            NotificationType.ProductionCompleted, "ProductionOrder", order.Id);

        return await GetOrderByIdAsync(id);
    }

    /// FEFO-deducts the given quantity of a product from any of the tenant's
    /// warehouses (earliest expiry first). Reserved stock is not touched.
    /// <remarks>
    /// F6: SQLite davridagi FEFO nusxasi o'rniga ombor modulining umumiy
    /// <see cref="IStockAllocator"/>i — transfer bilan bitta tartib, filtr SQL'da. Allocator
    /// SaveChanges chaqirmaydi: yechish bosqich yakuni bilan BITTA SaveChangesAsync'da yoziladi.
    /// Ishlab chiqarish xomashyoni HAMMA omborlardan oladi va partiya qoldig'ini ham kamaytiradi
    /// (tovar sarflanadi) — eski xulq aynan shu edi.
    /// </remarks>
    private async Task DeductFromStockAsync(Guid productId, decimal quantity)
    {
        if (quantity <= 0) return;

        var allocation = await _stock.DeductFefoAsync(productId, quantity, warehouseId: null, reduceBatchRemaining: true);
        var remaining = allocation.Shortfall;

        if (!allocation.IsSatisfied)
        {
            var productName = await _db.Products
                .Where(p => p.Id == productId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();
            // Shablon va qiymat alohida: xabar tarjima kaliti bo'lib qolsin.
            throw new AppException("Insufficient stock for input '{0}' (short by {1})",
                productName ?? productId.ToString(), Math.Round(remaining, 2));
        }
    }

    /// Creates a new batch + stock row for produced goods in the given warehouse.
    private async Task<Batch> AddProducedStockAsync(Guid warehouseId,
        Guid productId, decimal quantity, string lotPrefix)
    {
        var location = await _db.Locations.FirstOrDefaultAsync(l => l.WarehouseId == warehouseId);
        if (location == null)
        {
            location = new Location { WarehouseId = warehouseId, Name = "Default", Code = "DEF" };
            _db.Locations.Add(location);
        }

        var shelfLifeDays = await _db.Products
            .Where(p => p.Id == productId)
            .Select(p => p.ShelfLifeDays)
            .FirstOrDefaultAsync();
        var now = DateTime.UtcNow;
        var batch = new Batch
        {
            ProductId = productId,
            LotNumber = $"{lotPrefix}-{now:yyyyMMdd}-{Guid.NewGuid().ToString()[..6]}",
            ManufacturedDate = now,
            ExpiryDate = shelfLifeDays != null ? now.AddDays(shelfLifeDays.Value) : null,
            InitialQuantity = quantity, RemainingQuantity = quantity
        };
        _db.Batches.Add(batch);

        _db.WarehouseStocks.Add(new WarehouseStock
        {
            WarehouseId = warehouseId,
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
