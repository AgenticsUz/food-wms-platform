using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Seeding;

// Ishlab chiqarish, sifat, smena/KPI, yetkazish va bildirishnomalar.
internal sealed partial class DemoData
{
    private sealed record DemoRecipe(ProductionRecipe Recipe, Product Output, IReadOnlyList<(RecipeStage Stage, IReadOnlyList<RecipeStageItem> Inputs)> Steps);

    private sealed record DemoOrder(ProductionOrder Order, IReadOnlyList<StageExecution> Stages);

    private ProductionStage[] _stages = [];
    private DemoRecipe _plombirRecipe = null!;
    private DemoRecipe _shokoladRecipe = null!;
    private DemoRecipe _kremRecipe = null!;

    private QcParameter _qcTemperature = null!;
    private QcParameter _qcFat = null!;
    private QcParameter _qcSolids = null!;
    private QcParameter _qcAppearance = null!;
    private QcParameter _qcTaste = null!;

    private Shift _morning = null!;
    private Shift _dayShift = null!;
    private Vehicle _isuzu = null!;
    private Vehicle _labo = null!;
    private Driver _sherzod = null!;
    private Driver _bekzod = null!;

    // ── Ishlab chiqarish ──

    private void BuildProductionSetup()
    {
        string[] names = ["Aralashma tayyorlash", "Pishirish", "Sovutish", "Frezer", "Qadoqlash"];
        _stages = [.. names.Select((name, i) => Add(new ProductionStage { Name = name, OrderNumber = i + 1 }))];

        // Eski demo'da faqat Plombir retsepti bor edi, qolgan ikki mahsulot esa «hech qayerdan»
        // sotilardi. Endi har tayyor mahsulot o'z retsepti va buyurtmasidan keladi.
        _plombirRecipe = Recipe("Plombir 100ml Retsepti", _plombir, 1000,
            (1, _quruqSut, 10), (1, _shakar, 5), (1, _saryog, 3), (4, _stabilizator, 0.5m));
        _shokoladRecipe = Recipe("Shokoladli muzqaymoq Retsepti", _shokolad, 1000,
            (1, _quruqSut, 9), (1, _shakar, 6), (1, _kokos, 4), (4, _stabilizator, 0.5m));
        _kremRecipe = Recipe("Krem-brule Retsepti", _kremBrule, 1000,
            (1, _quyuqSut, 12), (1, _shakar, 7), (1, _saryog, 2), (4, _stabilizator, 0.4m));
    }

    /// <param name="inputs">(bosqich raqami, mahsulot, retseptning BITTA partiyasi — <paramref name="outputQuantity"/> — uchun miqdor).</param>
    private DemoRecipe Recipe(string name, Product output, decimal outputQuantity, params (int Stage, Product Product, decimal Quantity)[] inputs)
    {
        ProductionRecipe recipe = Add(new ProductionRecipe { Name = name, OutputProductId = output.Id, OutputQuantity = outputQuantity, OutputUnitId = output.UnitId });
        List<(RecipeStage, IReadOnlyList<RecipeStageItem>)> steps = [];

        for (int i = 0; i < _stages.Length; i++)
        {
            bool last = i == _stages.Length - 1;
            RecipeStage stage = Add(new RecipeStage
            {
                RecipeId = recipe.Id,
                StageId = _stages[i].Id,
                OrderNumber = i + 1,
                AllowWarehouseOutput = last,
                OutputWarehouseId = last ? _finishedWarehouse.Id : null,
            });

            List<RecipeStageItem> items = [.. inputs
                .Where(x => x.Stage == i + 1)
                .Select(x => Add(new RecipeStageItem { RecipeStageId = stage.Id, ProductId = x.Product.Id, Quantity = x.Quantity, UnitId = x.Product.UnitId }))];
            steps.Add((stage, items));
        }

        return new DemoRecipe(recipe, output, steps);
    }

    private DemoOrder CompletedOrder(DemoRecipe recipe, decimal planned, DateTime start, DateTime end, decimal actual, decimal waste, DateTime createdAt) =>
        Order(recipe, planned, start, end, ProductionOrderStatus.Completed, recipe.Steps.Count, actual, waste, createdAt);

    private DemoOrder InProgressOrder(DemoRecipe recipe, decimal planned, DateTime start, int completedStages, decimal actual, decimal waste, DateTime createdAt) =>
        Order(recipe, planned, start, null, ProductionOrderStatus.InProgress, completedStages, actual, waste, createdAt);

    private void DraftOrder(DemoRecipe recipe, decimal planned, DateTime start, DateTime end, DateTime createdAt) =>
        Order(recipe, planned, start, end, ProductionOrderStatus.Draft, 0, 0, 0, createdAt);

    /// <summary>
    /// Buyurtma va uning bosqichlari. Bosqich ijrolari buyurtma bilan birga ochiladi (servisdagidek);
    /// yakunlangan bosqich o'z kirimlarini (retsept × reja/partiya) FEFO bilan xom ashyodan oladi;
    /// yakunlangan buyurtma oxirgi bosqich natijasini tayyor omborga partiya + ProductionOutput
    /// transferi bilan kiritadi (<c>ProductionService.CompleteOrderAsync</c>).
    /// </summary>
    private DemoOrder Order(
        DemoRecipe recipe, decimal planned, DateTime start, DateTime? end, ProductionOrderStatus status,
        int completedStages, decimal actual, decimal waste, DateTime createdAt)
    {
        ProductionOrder order = Add(
            new ProductionOrder
            {
                RecipeId = recipe.Recipe.Id,
                PlannedQuantity = planned,
                Status = status,
                PlannedStartDate = start,
                PlannedEndDate = end,
                AssignedToUserId = _people.Employee.Id,
                Number = NextOrderNumber(),
            },
            createdAt);

        // Yakunlangan buyurtmada bosqichlar reja oralig'iga teng bo'linadi; jarayondagida — 3 soatdan.
        TimeSpan slice = end is { } finish && status == ProductionOrderStatus.Completed
            ? (finish - start) / recipe.Steps.Count
            : TimeSpan.FromHours(3);
        decimal factor = planned / recipe.Recipe.OutputQuantity;
        List<StageExecution> executions = [];

        // Sarflangan xomashyoning QIYMATI — tayyor mahsulot partiyasining tannarxi uchun (P2.5).
        decimal materialCost = 0m;

        for (int i = 0; i < recipe.Steps.Count; i++)
        {
            (RecipeStage stage, IReadOnlyList<RecipeStageItem> inputs) = recipe.Steps[i];
            StageExecution execution = Add(
                new StageExecution { ProductionOrderId = order.Id, RecipeStageId = stage.Id, PlannedQuantity = planned, Status = StageExecutionStatus.Pending },
                createdAt);
            executions.Add(execution);

            DateTime stageStart = start + (slice * i);
            if (i < completedStages)
            {
                execution.Status = StageExecutionStatus.Completed;
                execution.ActualQuantity = actual;
                execution.WasteQuantity = waste;
                execution.WorkerUserId = _people.Employee.Id;
                execution.StartTime = stageStart;
                execution.EndTime = stageStart + slice;

                decimal stageCost = 0m;
                foreach (RecipeStageItem input in inputs)
                {
                    foreach ((WarehouseStock stock, decimal taken) in Take(_productsById[input.ProductId], input.Quantity * factor, from: null))
                    {
                        stageCost += (_batches[stock.BatchId].UnitCost ?? 0m) * taken;
                    }
                }

                // Sarf qiymati BOSQICHDA saqlanadi (servisdagidek): buyurtma yakunlanganda
                // tayyor mahsulot tannarxi shu yig'indidan chiqadi. Kirimsiz bosqichda `null`.
                execution.MaterialCost = stageCost > 0m ? stageCost : null;
                materialCost += stageCost;
            }
            else if (i == completedStages && status == ProductionOrderStatus.InProgress)
            {
                execution.Status = StageExecutionStatus.InProgress;
                execution.WorkerUserId = _people.Employee.Id;
                execution.StartTime = stageStart;
            }
        }

        if (status == ProductionOrderStatus.InProgress)
        {
            Notify(null, "Ishlab chiqarish boshlandi", $"{recipe.Recipe.Name}: {Fmt(planned)} {UnitOf(recipe.Output)} rejalashtirildi.",
                NotificationType.ProductionStarted, "ProductionOrder", order.Id, start);
        }

        if (status == ProductionOrderStatus.Completed && end is { } completedAt)
        {
            Batch batch = NewBatch(recipe.Output, "PROD", completedAt, ExpiryFrom(recipe.Output, completedAt), actual, completedAt);

            // Tannarx — sarflangan xomashyo qiymati BIR BIRLIKKA. Chiqindi bo'luvchidan tashqarida:
            // yo'qotilgan xomashyo qiymati omon qolgan mahsulotga taqsimlanadi.
            batch.UnitCost = materialCost > 0 && actual > 0 ? Math.Round(materialCost / actual, 2) : null;
            AddStock(_finishedWarehouse, _locB1, recipe.Output, batch, actual, completedAt);

            Transfer output = Add(
                new Transfer
                {
                    Type = TransferType.ProductionOutput,
                    ToWarehouseId = _finishedWarehouse.Id,
                    Status = TransferStatus.Confirmed,
                    ConfirmedAt = completedAt,
                    Note = $"Ishlab chiqarish buyurtmasi: {recipe.Recipe.Name}, {Fmt(planned)} {UnitOf(recipe.Output)}",
                    DocumentDate = DocumentDay(completedAt),
                    Number = NextTransferNumber(),
                },
                completedAt);
            Item(output, recipe.Output, batch, actual, 0, completedAt);

            Notify(null, "Ishlab chiqarish yakunlandi",
                $"{Fmt(actual)} {UnitOf(recipe.Output)} {recipe.Output.Name} tayyor mahsulot omboriga kiritildi.",
                NotificationType.ProductionCompleted, "ProductionOrder", order.Id, completedAt);
        }

        return new DemoOrder(order, executions);
    }

    // ── Sifat nazorati (eski demo'da yo'q edi — QUALITY moduli bo'sh ochilmasin) ──

    private void BuildQualitySetup()
    {
        _qcTemperature = Add(new QcParameter { Name = "Harorat", Unit = "°C", MinValue = -25, MaxValue = -18, ValueType = QcParameterType.Numeric });
        _qcFat = Add(new QcParameter { Name = "Yog'lilik", Unit = "%", MinValue = 10, MaxValue = 16, ValueType = QcParameterType.Numeric });
        _qcSolids = Add(new QcParameter { Name = "Quruq modda", Unit = "%", MinValue = 36, MaxValue = 42, ValueType = QcParameterType.Numeric });
        _qcAppearance = Add(new QcParameter { Name = "Tashqi ko'rinish", ValueType = QcParameterType.Boolean });
        _qcTaste = Add(new QcParameter { Name = "Ta'm va hid", ValueType = QcParameterType.Text });
    }

    private void Check(StageExecution? stage, Transfer? transfer, QcParameter parameter, string value, bool passed, DateTime at, string? note = null) =>
        Add(
            new QcCheck
            {
                StageExecutionId = stage?.Id,
                TransferId = transfer?.Id,
                ParameterId = parameter.Id,
                Value = value,
                IsPassed = passed,
                CheckedByUserId = _people.Employee.Id,
                CheckedAt = at,
                Note = note,
            },
            at);

    // ── Smena va KPI ──

    private void BuildShifts()
    {
        _morning = Add(new Shift { Name = "Ertalabki smena", StartTime = new TimeSpan(6, 0, 0), EndTime = new TimeSpan(14, 0, 0) });
        _dayShift = Add(new Shift { Name = "Tushki smena", StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(22, 0, 0) });
        Add(new Shift { Name = "Tungi smena", StartTime = new TimeSpan(22, 0, 0), EndTime = new TimeSpan(6, 0, 0) });
    }

    /// <summary>
    /// Oxirgi 14 kun reja/fakt (eski demo: ertalabki smena, Plombir 800 reja). Qo'shimcha — tushki
    /// smena Shokoladli bilan, samaradorlik grafigi bitta chiziq bo'lmasin. KPI — hisobot, zaxira harakati EMAS.
    /// </summary>
    private void BuildShiftKpi()
    {
        // Qat'iy urug' (eski demo'dagi 42): qayta seed bir xil raqam bersin.
        Random rng = new(42);

        for (int d = 13; d >= 0; d--)
        {
            DateTime date = _localToday.AddDays(-d);
            DateTime at = Past(Day(d, 14));

            Add(new ShiftPlan { ShiftId = _morning.Id, ProductId = _plombir.Id, PlannedQuantity = 800, Date = date }, Day(d + 1, 17));
            Add(new ShiftActual { ShiftId = _morning.Id, ProductId = _plombir.Id, ActualQuantity = 720 + rng.Next(131), WasteQuantity = 10 + rng.Next(31), Date = date }, at);

            Add(new ShiftPlan { ShiftId = _dayShift.Id, ProductId = _shokolad.Id, PlannedQuantity = 400, Date = date }, Day(d + 1, 17));
            Add(new ShiftActual { ShiftId = _dayShift.Id, ProductId = _shokolad.Id, ActualQuantity = 340 + rng.Next(81), WasteQuantity = 5 + rng.Next(16), Date = date }, at);
        }
    }

    /// <summary>Davomat: xodim — ertalabki smena (PIN), menejer — tushki (FaceID); yakshanba dam, bugun hali yozilmagan.</summary>
    private void BuildAttendance()
    {
        for (int d = 13; d >= 1; d--)
        {
            if (_localToday.AddDays(-d).DayOfWeek == DayOfWeek.Sunday)
            {
                continue;
            }

            DateTime morningIn = Day(d, 5, 50 + (d % 4 * 5));
            Add(new AttendanceLog { UserId = _people.Employee.Id, ShiftId = _morning.Id, CheckIn = morningIn, CheckOut = Day(d, 14, 5), Method = AttendanceMethod.PIN, DeviceId = "KIOSK-01" }, morningIn);

            DateTime dayIn = Day(d, 13, 50 + (d % 3 * 5));
            Add(new AttendanceLog { UserId = _people.Manager.Id, ShiftId = _dayShift.Id, CheckIn = dayIn, CheckOut = Day(d, 22, 10), Method = AttendanceMethod.FaceID, DeviceId = "KIOSK-01" }, dayIn);
        }
    }

    // ── Yetkazish (eski demo'da yo'q edi) ──

    private void BuildFleet()
    {
        _isuzu = Add(new Vehicle { Name = "01 A 123 BC", Model = "Isuzu NPR (refrijerator)", Capacity = 3000 });
        _labo = Add(new Vehicle { Name = "01 B 456 CD", Model = "Chevrolet Labo", Capacity = 800 });
        _sherzod = Add(new Driver { FullName = "Sherzod Aliqulov", Phone = "+998907771122", LicenseNumber = "AF1234567" });
        _bekzod = Add(new Driver { FullName = "Bekzod Rahimov", Phone = "+998907773344", LicenseNumber = "AB7654321" });
    }

    private void BuildDeliveries(Transfer s1, Transfer s2, Transfer s3, Transfer s4, Transfer s5)
    {
        // Har tasdiqlangan sotuv yetkazilgan; ertangi reys — hali transferi yo'q haftalik buyurtmalar.
        AddDelivery(_isuzu, _sherzod, DeliveryStatus.Completed, Day(17, 9), (_korzinka, s1, DeliveryStopStatus.Delivered, Day(17, 11)));
        AddDelivery(_isuzu, _sherzod, DeliveryStatus.Completed, Day(14, 9), (_makro, s2, DeliveryStopStatus.Delivered, Day(14, 11)));
        AddDelivery(_labo, _bekzod, DeliveryStatus.Completed, Day(12, 9), (_korzinka, s3, DeliveryStopStatus.Delivered, Day(12, 12)));
        AddDelivery(_labo, _bekzod, DeliveryStatus.Completed, Day(8, 9), (_plov, s4, DeliveryStopStatus.Delivered, Day(8, 11)));
        AddDelivery(_isuzu, _sherzod, DeliveryStatus.Completed, Day(4, 9), (_makro, s5, DeliveryStopStatus.Delivered, Day(4, 11)));
        AddDelivery(_isuzu, _bekzod, DeliveryStatus.Planned, Day(-1, 9),
            (_korzinka, null, DeliveryStopStatus.Pending, null),
            (_plov, null, DeliveryStopStatus.Pending, null));
    }

    private void AddDelivery(Vehicle vehicle, Driver driver, DeliveryStatus status, DateTime scheduled,
        params (Counterparty Client, Transfer? Sale, DeliveryStopStatus Status, DateTime? DeliveredAt)[] stops)
    {
        DateTime createdAt = Past(scheduled.AddDays(-1));
        Delivery delivery = Add(
            new Delivery
            {
                VehicleId = vehicle.Id,
                DriverId = driver.Id,
                Status = status,
                ScheduledDate = scheduled,
                CreatedByUserId = _people.Manager.Id,
                Note = status == DeliveryStatus.Planned ? "Haftalik buyurtmalar — yuk transferi yuklashda tuziladi" : null,
            },
            createdAt);

        for (int i = 0; i < stops.Length; i++)
        {
            (Counterparty client, Transfer? sale, DeliveryStopStatus stopStatus, DateTime? deliveredAt) = stops[i];
            Add(
                new DeliveryStop
                {
                    DeliveryId = delivery.Id,
                    CounterpartyId = client.Id,
                    TransferId = sale?.Id,
                    Address = client.Address,
                    SequenceOrder = i + 1,
                    Status = stopStatus,
                    DeliveredAt = deliveredAt,
                },
                createdAt);
        }
    }

    // ── Bildirishnomalar ──

    /// <summary>Kam qoldiq — sotuv tasdiqlanganda servis yozadigan ogohlantirish (raqam daftardan, qotirilmagan).</summary>
    private void LowStockAlert(Product product, DateTime at)
    {
        decimal stock = StockOf(product);
        if (product.MinStock > 0 && stock < product.MinStock)
        {
            Notify(null, $"Kam qoldiq: {product.Name}",
                $"Omborda {Fmt(stock)} {UnitOf(product)} qoldi — minimum {Fmt(product.MinStock)} {UnitOf(product)}.",
                NotificationType.LowStock, "Product", product.Id, at);
        }
    }

    /// <summary>Uch kundan eskisi o'qilgan — qo'ng'iroqcha 15 ta eski xabar bilan ochilmasin.</summary>
    private void Notify(UserProfile? user, string title, string message, NotificationType type, string entityType, Guid entityId, DateTime at)
    {
        bool read = at < _now.AddDays(-3);
        Add(
            new Notification
            {
                UserId = user?.Id,
                Title = title,
                Message = message,
                Type = type,
                EntityType = entityType,
                EntityId = entityId,
                IsRead = read,
                ReadAt = read ? at.AddHours(2) : null,
            },
            at);
    }
}
