using Microsoft.EntityFrameworkCore;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Persistence;

public static class DataInitializer
{
    public static async Task SeedAsync(WmsDbContext db)
    {
        if (await db.Tenants.AnyAsync())
        {
            await EnsureAdminPermissionsAsync(db);
            await SeedDemoDataAsync(db);
            return;
        }

        // 1. Create default tenant
        var tenant = new Tenant
        {
            Name = "WMS Admin",
            Slug = "admin",
            IsActive = true
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        // 2. Assign all 9 modules to this tenant
        var modules = await db.Modules.ToListAsync();
        foreach (var module in modules)
        {
            db.TenantModules.Add(new TenantModule
            {
                TenantId = tenant.Id,
                ModuleId = module.Id,
                IsEnabled = true
            });
        }
        await db.SaveChangesAsync();

        // 3. Create default Admin role
        var role = new Role
        {
            TenantId = tenant.Id,
            Name = "Admin",
            Description = "System administrator with full access"
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        // 4. Create default admin user
        var user = new User
        {
            TenantId = tenant.Id,
            FullName = "Admin",
            Phone = "998901234567",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123456"),
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // 5. Assign Admin role to the admin user
        db.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });
        await db.SaveChangesAsync();

        // 6. Assign ALL permissions to Admin role
        await AssignAllPermissionsToRole(db, role.Id);

        // 7. Seed demo data
        await SeedDemoDataAsync(db);
    }

    private static async Task SeedDemoDataAsync(WmsDbContext db)
    {
        var productCount = await db.Products.CountAsync();
        if (productCount >= 5) return;

        const int tenantId = 1;
        var now = DateTime.UtcNow;
        var adminUser = await db.Users.FirstAsync(u => u.TenantId == tenantId);
        var rng = new Random(42); // deterministic seed

        // ── UNITS ──
        var unitKg = new Unit { TenantId = tenantId, Name = "Kilogramm", ShortName = "kg" };
        var unitLitr = new Unit { TenantId = tenantId, Name = "Litr", ShortName = "litr" };
        var unitDona = new Unit { TenantId = tenantId, Name = "Dona", ShortName = "dona" };
        var unitGramm = new Unit { TenantId = tenantId, Name = "Gramm", ShortName = "gramm" };
        var unitQuti = new Unit { TenantId = tenantId, Name = "Quti", ShortName = "quti" };
        var unitPaket = new Unit { TenantId = tenantId, Name = "Paket", ShortName = "paket" };
        db.Units.AddRange(unitKg, unitLitr, unitDona, unitGramm, unitQuti, unitPaket);
        await db.SaveChangesAsync();

        // ── CATEGORIES ──
        var catXom = new Category { TenantId = tenantId, Name = "Xom ashyo" };
        var catTayyor = new Category { TenantId = tenantId, Name = "Tayyor mahsulot" };
        var catQadoq = new Category { TenantId = tenantId, Name = "Qadoqlash" };
        db.Categories.AddRange(catXom, catTayyor, catQadoq);
        await db.SaveChangesAsync();

        var catSut = new Category { TenantId = tenantId, Name = "Sut mahsulotlari", ParentId = catXom.Id };
        var catQand = new Category { TenantId = tenantId, Name = "Qand va shakar", ParentId = catXom.Id };
        var catYog = new Category { TenantId = tenantId, Name = "Yog'lar", ParentId = catXom.Id };
        var catMuz = new Category { TenantId = tenantId, Name = "Muzqaymoq", ParentId = catTayyor.Id };
        var catQandolat = new Category { TenantId = tenantId, Name = "Qandolat", ParentId = catTayyor.Id };
        db.Categories.AddRange(catSut, catQand, catYog, catMuz, catQandolat);
        await db.SaveChangesAsync();

        // ── PRODUCTS — Raw ──
        var pQuruqSut = new Product { TenantId = tenantId, Name = "Quruq sut", CategoryId = catSut.Id, UnitId = unitKg.Id, Type = ProductType.Raw, MinStock = 100, CostPrice = 25000 };
        var pQuyuqSut = new Product { TenantId = tenantId, Name = "Quyuq sut", CategoryId = catSut.Id, UnitId = unitLitr.Id, Type = ProductType.Raw, MinStock = 50, CostPrice = 8000 };
        var pShakar = new Product { TenantId = tenantId, Name = "Shakar", CategoryId = catQand.Id, UnitId = unitKg.Id, Type = ProductType.Raw, MinStock = 200, CostPrice = 12000 };
        var pSaryog = new Product { TenantId = tenantId, Name = "Saryog'", CategoryId = catYog.Id, UnitId = unitKg.Id, Type = ProductType.Raw, MinStock = 30, CostPrice = 45000 };
        var pKokos = new Product { TenantId = tenantId, Name = "Kokos yog'i", CategoryId = catYog.Id, UnitId = unitKg.Id, Type = ProductType.Raw, MinStock = 20, CostPrice = 38000 };
        var pStabilizator = new Product { TenantId = tenantId, Name = "Stabilizator", CategoryId = catXom.Id, UnitId = unitKg.Id, Type = ProductType.Raw, MinStock = 10, CostPrice = 95000 };
        db.Products.AddRange(pQuruqSut, pQuyuqSut, pShakar, pSaryog, pKokos, pStabilizator);
        await db.SaveChangesAsync();

        // ── PRODUCTS — Finished ──
        var pPlombir = new Product { TenantId = tenantId, Name = "Plombir 100ml", CategoryId = catMuz.Id, UnitId = unitDona.Id, Type = ProductType.Finished, MinStock = 500, CostPrice = 3500 };
        var pShokMuz = new Product { TenantId = tenantId, Name = "Shokoladli muzqaymoq", CategoryId = catMuz.Id, UnitId = unitDona.Id, Type = ProductType.Finished, MinStock = 300, CostPrice = 4200 };
        var pKremBrule = new Product { TenantId = tenantId, Name = "Krem-brule", CategoryId = catMuz.Id, UnitId = unitDona.Id, Type = ProductType.Finished, MinStock = 200, CostPrice = 3800 };
        db.Products.AddRange(pPlombir, pShokMuz, pKremBrule);
        await db.SaveChangesAsync();

        // ── WAREHOUSES ──
        var whRaw = new Warehouse { TenantId = tenantId, Name = "Xom ashyo ombori", Type = WarehouseType.Raw };
        var whFinished = new Warehouse { TenantId = tenantId, Name = "Tayyor mahsulot ombori", Type = WarehouseType.Finished };
        db.Warehouses.AddRange(whRaw, whFinished);
        await db.SaveChangesAsync();

        // ── LOCATIONS ──
        var locA1 = new Location { WarehouseId = whRaw.Id, Name = "Sovutgich A-1", Code = "A1" };
        var locA2 = new Location { WarehouseId = whRaw.Id, Name = "Sovutgich A-2", Code = "A2" };
        var locB1 = new Location { WarehouseId = whFinished.Id, Name = "Muzlatgich B-1", Code = "B1" };
        var locB2 = new Location { WarehouseId = whFinished.Id, Name = "Muzlatgich B-2", Code = "B2" };
        db.Locations.AddRange(locA1, locA2, locB1, locB2);
        await db.SaveChangesAsync();

        // ── COUNTERPARTIES — Suppliers ──
        var supNemat = new Counterparty { TenantId = tenantId, Name = "Nemat Agro", Phone = "998901111111", Type = CounterpartyType.Supplier };
        var supBaraka = new Counterparty { TenantId = tenantId, Name = "Baraka Sut zavodi", Phone = "998902222222", Type = CounterpartyType.Supplier };
        var supSharq = new Counterparty { TenantId = tenantId, Name = "Sharq Savdo", Phone = "998903333333", Type = CounterpartyType.Supplier };
        db.Counterparties.AddRange(supNemat, supBaraka, supSharq);
        await db.SaveChangesAsync();

        // ── COUNTERPARTIES — Clients ──
        var cliKorzinka = new Counterparty { TenantId = tenantId, Name = "Supermarket Korzinka", Phone = "998904444444", Type = CounterpartyType.Client };
        var cliMakro = new Counterparty { TenantId = tenantId, Name = "Do'kon Makro", Phone = "998905555555", Type = CounterpartyType.Client };
        var cliPlov = new Counterparty { TenantId = tenantId, Name = "Restoran Plov Markazi", Phone = "998906666666", Type = CounterpartyType.Client };
        db.Counterparties.AddRange(cliKorzinka, cliMakro, cliPlov);
        await db.SaveChangesAsync();

        // ── PRODUCTION STAGES ──
        var stage1 = new ProductionStage { TenantId = tenantId, Name = "Aralashma tayyorlash", OrderNumber = 1 };
        var stage2 = new ProductionStage { TenantId = tenantId, Name = "Pishirish", OrderNumber = 2 };
        var stage3 = new ProductionStage { TenantId = tenantId, Name = "Sovutish", OrderNumber = 3 };
        var stage4 = new ProductionStage { TenantId = tenantId, Name = "Frezer", OrderNumber = 4 };
        var stage5 = new ProductionStage { TenantId = tenantId, Name = "Qadoqlash", OrderNumber = 5 };
        db.ProductionStages.AddRange(stage1, stage2, stage3, stage4, stage5);
        await db.SaveChangesAsync();

        // ── RECIPE — Plombir 100ml ──
        var recipe = new ProductionRecipe
        {
            TenantId = tenantId, Name = "Plombir 100ml Retsepti",
            OutputProductId = pPlombir.Id, OutputQuantity = 1000, OutputUnitId = unitDona.Id
        };
        db.ProductionRecipes.Add(recipe);
        await db.SaveChangesAsync();

        // Recipe stages
        var rs1 = new RecipeStage { RecipeId = recipe.Id, StageId = stage1.Id, OrderNumber = 1 };
        var rs2 = new RecipeStage { RecipeId = recipe.Id, StageId = stage2.Id, OrderNumber = 2 };
        var rs3 = new RecipeStage { RecipeId = recipe.Id, StageId = stage3.Id, OrderNumber = 3 };
        var rs4 = new RecipeStage { RecipeId = recipe.Id, StageId = stage4.Id, OrderNumber = 4 };
        var rs5 = new RecipeStage
        {
            RecipeId = recipe.Id, StageId = stage5.Id, OrderNumber = 5,
            AllowWarehouseOutput = true, OutputWarehouseId = whFinished.Id
        };
        db.RecipeStages.AddRange(rs1, rs2, rs3, rs4, rs5);
        await db.SaveChangesAsync();

        // Recipe stage inputs
        db.RecipeStageItems.AddRange(
            new RecipeStageItem { RecipeStageId = rs1.Id, ProductId = pQuruqSut.Id, Quantity = 10, UnitId = unitKg.Id },
            new RecipeStageItem { RecipeStageId = rs1.Id, ProductId = pShakar.Id, Quantity = 5, UnitId = unitKg.Id },
            new RecipeStageItem { RecipeStageId = rs1.Id, ProductId = pSaryog.Id, Quantity = 3, UnitId = unitKg.Id },
            new RecipeStageItem { RecipeStageId = rs4.Id, ProductId = pStabilizator.Id, Quantity = 0.5m, UnitId = unitKg.Id }
        );
        await db.SaveChangesAsync();

        // ── HELPER: Create confirmed incoming transfer ──
        async Task<Transfer> CreateIncomingTransfer(int supplierId, int productId, decimal qty, decimal price, int daysAgo)
        {
            var batch = new Batch
            {
                TenantId = tenantId, ProductId = productId,
                LotNumber = $"LOT-{now.AddDays(-daysAgo):yyyyMMdd}-{productId}-{Guid.NewGuid().ToString()[..4]}",
                ManufacturedDate = now.AddDays(-daysAgo),
                ExpiryDate = now.AddDays(-daysAgo + 180),
                InitialQuantity = qty, RemainingQuantity = qty
            };
            db.Batches.Add(batch);
            await db.SaveChangesAsync();

            var transfer = new Transfer
            {
                TenantId = tenantId, Type = TransferType.Incoming,
                ToWarehouseId = whRaw.Id, CounterpartyId = supplierId,
                CreatedByUserId = adminUser.Id, Status = TransferStatus.Confirmed,
                ConfirmedAt = now.AddDays(-daysAgo),
                CreatedAt = now.AddDays(-daysAgo)
            };
            db.Transfers.Add(transfer);
            await db.SaveChangesAsync();

            db.TransferItems.Add(new TransferItem
            {
                TransferId = transfer.Id, ProductId = productId,
                BatchId = batch.Id, Quantity = qty, UnitPrice = price
            });
            await db.SaveChangesAsync();

            // Add stock
            db.WarehouseStocks.Add(new WarehouseStock
            {
                TenantId = tenantId, WarehouseId = whRaw.Id,
                LocationId = locA1.Id, ProductId = productId,
                BatchId = batch.Id, Quantity = qty
            });
            await db.SaveChangesAsync();

            return transfer;
        }

        // ── HELPER: Create confirmed outgoing transfer ──
        async Task CreateOutgoingTransfer(int clientId, int productId, decimal qty, decimal price, int daysAgo)
        {
            // Create batch for finished product if needed
            var existingBatch = await db.Batches.FirstOrDefaultAsync(b => b.TenantId == tenantId && b.ProductId == productId);
            if (existingBatch == null)
            {
                existingBatch = new Batch
                {
                    TenantId = tenantId, ProductId = productId,
                    LotNumber = $"PROD-{productId}-{Guid.NewGuid().ToString()[..4]}",
                    ManufacturedDate = now.AddDays(-daysAgo - 5),
                    ExpiryDate = now.AddDays(365),
                    InitialQuantity = 5000, RemainingQuantity = 5000
                };
                db.Batches.Add(existingBatch);
                await db.SaveChangesAsync();

                // Add initial finished goods stock
                db.WarehouseStocks.Add(new WarehouseStock
                {
                    TenantId = tenantId, WarehouseId = whFinished.Id,
                    LocationId = locB1.Id, ProductId = productId,
                    BatchId = existingBatch.Id, Quantity = 5000
                });
                await db.SaveChangesAsync();
            }

            var transfer = new Transfer
            {
                TenantId = tenantId, Type = TransferType.Outgoing,
                FromWarehouseId = whFinished.Id, CounterpartyId = clientId,
                CreatedByUserId = adminUser.Id, Status = TransferStatus.Confirmed,
                ConfirmedAt = now.AddDays(-daysAgo),
                CreatedAt = now.AddDays(-daysAgo)
            };
            db.Transfers.Add(transfer);
            await db.SaveChangesAsync();

            db.TransferItems.Add(new TransferItem
            {
                TransferId = transfer.Id, ProductId = productId,
                BatchId = existingBatch.Id, Quantity = qty, UnitPrice = price
            });
            await db.SaveChangesAsync();

            // Reduce stock
            var stock = await db.WarehouseStocks.FirstAsync(s =>
                s.TenantId == tenantId && s.WarehouseId == whFinished.Id && s.ProductId == productId);
            stock.Quantity -= qty;
            existingBatch.RemainingQuantity -= qty;
            await db.SaveChangesAsync();

            // Update debt
            var debt = await db.Debts.FirstOrDefaultAsync(d => d.TenantId == tenantId && d.CounterpartyId == clientId);
            if (debt == null)
            {
                debt = new Debt { TenantId = tenantId, CounterpartyId = clientId, Amount = 0 };
                db.Debts.Add(debt);
            }
            debt.Amount += qty * price;
            await db.SaveChangesAsync();
        }

        // ── INCOMING TRANSFERS ──
        await CreateIncomingTransfer(supNemat.Id, pQuruqSut.Id, 500, 25000, 25);
        await CreateIncomingTransfer(supBaraka.Id, pQuyuqSut.Id, 200, 8000, 20);
        await CreateIncomingTransfer(supSharq.Id, pShakar.Id, 300, 12000, 18);
        await CreateIncomingTransfer(supNemat.Id, pSaryog.Id, 50, 45000, 15);
        await CreateIncomingTransfer(supBaraka.Id, pQuruqSut.Id, 300, 25000, 10);
        await CreateIncomingTransfer(supSharq.Id, pKokos.Id, 30, 38000, 7);
        await CreateIncomingTransfer(supNemat.Id, pStabilizator.Id, 15, 95000, 3);

        // ── OUTGOING TRANSFERS ──
        await CreateOutgoingTransfer(cliKorzinka.Id, pPlombir.Id, 500, 5000, 22);
        await CreateOutgoingTransfer(cliMakro.Id, pPlombir.Id, 300, 5000, 17);
        await CreateOutgoingTransfer(cliKorzinka.Id, pShokMuz.Id, 200, 6000, 12);
        await CreateOutgoingTransfer(cliPlov.Id, pKremBrule.Id, 150, 5500, 8);
        await CreateOutgoingTransfer(cliMakro.Id, pPlombir.Id, 400, 5000, 4);

        // ── PRODUCTION ORDERS ──
        // Order 1: Completed, 20 days ago
        var order1 = new ProductionOrder
        {
            TenantId = tenantId, RecipeId = recipe.Id, PlannedQuantity = 1000,
            Status = ProductionOrderStatus.Completed, PlannedStartDate = now.AddDays(-20),
            PlannedEndDate = now.AddDays(-18), AssignedToUserId = adminUser.Id,
            CreatedAt = now.AddDays(-20)
        };
        db.ProductionOrders.Add(order1);
        await db.SaveChangesAsync();

        foreach (var rs in new[] { rs1, rs2, rs3, rs4, rs5 })
        {
            db.StageExecutions.Add(new StageExecution
            {
                ProductionOrderId = order1.Id, RecipeStageId = rs.Id,
                PlannedQuantity = 1000, ActualQuantity = 980, WasteQuantity = 20,
                WorkerUserId = adminUser.Id, Status = StageExecutionStatus.Completed,
                StartTime = now.AddDays(-20), EndTime = now.AddDays(-18)
            });
        }
        await db.SaveChangesAsync();

        // Order 2: Completed, 10 days ago
        var order2 = new ProductionOrder
        {
            TenantId = tenantId, RecipeId = recipe.Id, PlannedQuantity = 500,
            Status = ProductionOrderStatus.Completed, PlannedStartDate = now.AddDays(-10),
            PlannedEndDate = now.AddDays(-8), AssignedToUserId = adminUser.Id,
            CreatedAt = now.AddDays(-10)
        };
        db.ProductionOrders.Add(order2);
        await db.SaveChangesAsync();

        foreach (var rs in new[] { rs1, rs2, rs3, rs4, rs5 })
        {
            db.StageExecutions.Add(new StageExecution
            {
                ProductionOrderId = order2.Id, RecipeStageId = rs.Id,
                PlannedQuantity = 500, ActualQuantity = 485, WasteQuantity = 15,
                WorkerUserId = adminUser.Id, Status = StageExecutionStatus.Completed,
                StartTime = now.AddDays(-10), EndTime = now.AddDays(-8)
            });
        }
        await db.SaveChangesAsync();

        // Order 3: InProgress, 2 days ago
        var order3 = new ProductionOrder
        {
            TenantId = tenantId, RecipeId = recipe.Id, PlannedQuantity = 300,
            Status = ProductionOrderStatus.InProgress, PlannedStartDate = now.AddDays(-2),
            AssignedToUserId = adminUser.Id, CreatedAt = now.AddDays(-2)
        };
        db.ProductionOrders.Add(order3);
        await db.SaveChangesAsync();

        // ── SHIFTS ──
        var shiftMorning = new Shift { TenantId = tenantId, Name = "Ertalabki smena", StartTime = new TimeSpan(6, 0, 0), EndTime = new TimeSpan(14, 0, 0) };
        var shiftDay = new Shift { TenantId = tenantId, Name = "Tushki smena", StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(22, 0, 0) };
        var shiftNight = new Shift { TenantId = tenantId, Name = "Tungi smena", StartTime = new TimeSpan(22, 0, 0), EndTime = new TimeSpan(6, 0, 0) };
        db.Shifts.AddRange(shiftMorning, shiftDay, shiftNight);
        await db.SaveChangesAsync();

        // ── SHIFT PLANS + ACTUALS (last 14 days) ──
        for (int d = 13; d >= 0; d--)
        {
            var date = now.AddDays(-d).Date;

            db.ShiftPlans.Add(new ShiftPlan
            {
                TenantId = tenantId, ShiftId = shiftMorning.Id,
                ProductId = pPlombir.Id, PlannedQuantity = 800, Date = date
            });

            var actual = 720 + rng.Next(131); // 720..850
            var waste = 10 + rng.Next(31); // 10..40
            db.ShiftActuals.Add(new ShiftActual
            {
                TenantId = tenantId, ShiftId = shiftMorning.Id,
                ProductId = pPlombir.Id, ActualQuantity = actual, WasteQuantity = waste, Date = date
            });
        }
        await db.SaveChangesAsync();

        // ── TRANSACTIONS — Income ──
        db.Transactions.AddRange(
            new Transaction { TenantId = tenantId, Type = TransactionType.Income, CounterpartyId = cliKorzinka.Id, Amount = 3_500_000, Description = "Korzinka to'lovi", Date = now.AddDays(-21), RecordedByUserId = adminUser.Id },
            new Transaction { TenantId = tenantId, Type = TransactionType.Income, CounterpartyId = cliMakro.Id, Amount = 2_800_000, Description = "Makro to'lovi", Date = now.AddDays(-16), RecordedByUserId = adminUser.Id },
            new Transaction { TenantId = tenantId, Type = TransactionType.Income, CounterpartyId = cliKorzinka.Id, Amount = 1_500_000, Description = "Korzinka to'lovi", Date = now.AddDays(-11), RecordedByUserId = adminUser.Id },
            new Transaction { TenantId = tenantId, Type = TransactionType.Income, CounterpartyId = cliPlov.Id, Amount = 825_000, Description = "Plov Markazi to'lovi", Date = now.AddDays(-7), RecordedByUserId = adminUser.Id },
            new Transaction { TenantId = tenantId, Type = TransactionType.Income, CounterpartyId = cliMakro.Id, Amount = 2_000_000, Description = "Makro to'lovi", Date = now.AddDays(-3), RecordedByUserId = adminUser.Id }
        );
        await db.SaveChangesAsync();

        // ── TRANSACTIONS — Expense ──
        db.Transactions.AddRange(
            new Transaction { TenantId = tenantId, Type = TransactionType.Expense, Amount = 3_500_000, Description = "Ishchi oylik", Date = now.AddDays(-30), RecordedByUserId = adminUser.Id },
            new Transaction { TenantId = tenantId, Type = TransactionType.Expense, Amount = 850_000, Description = "Elektr energiya", Date = now.AddDays(-25), RecordedByUserId = adminUser.Id },
            new Transaction { TenantId = tenantId, Type = TransactionType.Expense, Amount = 1_200_000, Description = "Qadoqlash materiallari", Date = now.AddDays(-18), RecordedByUserId = adminUser.Id },
            new Transaction { TenantId = tenantId, Type = TransactionType.Expense, Amount = 450_000, Description = "Transport", Date = now.AddDays(-12), RecordedByUserId = adminUser.Id },
            new Transaction { TenantId = tenantId, Type = TransactionType.Expense, Amount = 380_000, Description = "Kommunal xizmatlar", Date = now.AddDays(-6), RecordedByUserId = adminUser.Id }
        );
        await db.SaveChangesAsync();

        // Adjust debts for income payments
        foreach (var tx in await db.Transactions.Where(t => t.TenantId == tenantId && t.Type == TransactionType.Income && t.CounterpartyId != null).ToListAsync())
        {
            var debt = await db.Debts.FirstOrDefaultAsync(d => d.TenantId == tenantId && d.CounterpartyId == tx.CounterpartyId);
            if (debt != null)
                debt.Amount -= tx.Amount;
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsureAdminPermissionsAsync(WmsDbContext db)
    {
        var adminRoles = await db.Roles.Where(r => r.Name == "Admin").ToListAsync();
        var allPermissionIds = await db.Permissions.Select(p => p.Id).ToListAsync();

        foreach (var role in adminRoles)
        {
            var existingPermIds = await db.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var missingIds = allPermissionIds.Except(existingPermIds).ToList();
            if (missingIds.Count == 0) continue;

            foreach (var permId in missingIds)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId
                });
            }
            await db.SaveChangesAsync();
        }
    }

    private static async Task AssignAllPermissionsToRole(WmsDbContext db, int roleId)
    {
        var permissions = await db.Permissions.ToListAsync();
        foreach (var permission in permissions)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permission.Id
            });
        }
        await db.SaveChangesAsync();
    }
}
