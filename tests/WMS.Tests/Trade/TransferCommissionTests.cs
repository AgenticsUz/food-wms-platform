using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Transfers;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Trade;

/// <summary>
/// Agentli chiqim tasdiqlanganda komissiya yozuvi: summasi, yaxlitlashi va
/// TAKRORLANMASLIGI.
/// </summary>
/// <remarks>
/// <para>
/// Nima uchun kerak (reja §P1). Komissiya — agentga to'lanadigan PUL, ya'ni dublikat
/// yozuv to'g'ridan-to'g'ri ortiqcha to'lov degani. Uni uchta qatlam qo'riqlaydi va
/// testlar uchalasini ham ushlaydi:
/// </para>
/// <list type="number">
/// <item><c>Math.Round(sale * percent / 100, 2)</c> — pul ustuni <c>numeric(18,2)</c>;
/// yaxlitlash yo'qolsa baza qiymatni o'zi kesib, hisobot bilan yozuv farq qilardi.</item>
/// <item>Hujjat holati — tasdiqlangan hujjat ikkinchi marta tasdiqlanmaydi.</item>
/// <item><c>(tenant_id, transfer_id, agent_id)</c> NOYOB indeksi (D13) — SQLite davrida
/// dublikatni faqat servisdagi <c>AnyAsync</c> to'sardi va parallel tasdiqda IKKALASI
/// ham o'tardi. Indeks tushib qolsa oxirgi test qizil beradi.</item>
/// </list>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class TransferCommissionTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public TransferCommissionTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    /// <summary>Noyob kodli yangi test tenanti.</summary>
    /// <returns>Tenant.</returns>
    /// <remarks>
    /// ⚠️ Kod OSHKORA beriladi — sababi <see cref="TransferServiceTests"/> dagi bir xil
    /// yordamchida (fixture'ning sukut kodi vaqt tamg'asidan yasaladi va to'qnashadi).
    /// </remarks>
    private Task<TestTenant> NewTenantAsync() =>
        _fixture.CreateTenantAsync($"t-{Guid.NewGuid().ToString("N")[..12]}");

    [Fact]
    public async Task Agentli_chiqim_tasdiqlansa_komissiya_yozuvi_yaratiladi()
    {
        TestTenant tenant = await NewTenantAsync();
        Guid agentId;

        await using (WmsTenantScope scope = _fixture.BeginScope(tenant))
        {
            (Guid transferId, Guid agent, _) = await CreateAgentSaleAsync(scope);
            agentId = agent;
            await scope.Service<ITransferService>().ConfirmAsync(transferId);
        }

        await using WmsTenantScope verify = _fixture.BeginScope(tenant);

        CommissionRecord record = await verify.Db.CommissionRecords
            .SingleAsync(TestContext.Current.CancellationToken);

        record.AgentId.ShouldBe(agentId);
        record.SaleAmount.ShouldBe(4501.65m);      // 3 × 1500.55
        record.CommissionPercent.ShouldBe(12.5m);  // hujjatdagi override agentning 10 % idan ustun
        record.CommissionAmount.ShouldBe(562.71m); // round(4501.65 × 12.5 / 100, 2) = round(562.70625, 2)
        record.Status.ShouldBe(CommissionStatus.Pending);
        record.IsPaid.ShouldBeFalse();
    }

    [Fact]
    public async Task Tasdiqlangan_hujjat_ikkinchi_marta_tasdiqlanmaydi_va_komissiya_takrorlanmaydi()
    {
        TestTenant tenant = await NewTenantAsync();

        await using WmsTenantScope scope = _fixture.BeginScope(tenant);
        (Guid transferId, _, _) = await CreateAgentSaleAsync(scope);

        ITransferService transfers = scope.Service<ITransferService>();
        await transfers.ConfirmAsync(transferId);

        AppException error = await Should.ThrowAsync<AppException>(async () =>
            await transfers.ConfirmAsync(transferId));
        error.MessageTemplate.ShouldBe("Only pending transfers can be confirmed");

        await using WmsTenantScope verify = _fixture.BeginScope(tenant);

        int records = await verify.Db.CommissionRecords.CountAsync(TestContext.Current.CancellationToken);
        records.ShouldBe(1);

        // Zaxira ham bir marta kamaygan bo'lsin: ikkinchi tasdiq hech narsaga tegmadi.
        decimal quantity = await verify.Db.WarehouseStocks
            .Select(s => s.Quantity)
            .SingleAsync(TestContext.Current.CancellationToken);
        quantity.ShouldBe(7m);
    }

    [Fact]
    public async Task Bir_sotuvga_ikkinchi_komissiya_yozuvi_noyob_indeks_bilan_toxtatiladi()
    {
        TestTenant tenant = await NewTenantAsync();
        Guid transferId;
        Guid agentId;

        await using (WmsTenantScope scope = _fixture.BeginScope(tenant))
        {
            (transferId, agentId, _) = await CreateAgentSaleAsync(scope);
            await scope.Service<ITransferService>().ConfirmAsync(transferId);
        }

        // Servisdagi `AnyAsync` tekshiruvi — BIRINCHI to'r, oxirgisi emas. Bu yerda u
        // ATAYLAB chetlab o'tiladi (yozuv to'g'ridan-to'g'ri qo'shiladi), chunki parallel
        // ikki tasdiqda ikkala so'rov ham «yo'q» deb ko'rishi mumkin. Oxirgi so'zni baza
        // aytishi SHART — indeks tushib qolsa bu da'vo qizil beradi.
        await using WmsTenantScope duplicate = _fixture.BeginScope(tenant);

        duplicate.Db.CommissionRecords.Add(new CommissionRecord
        {
            AgentId = agentId,
            TransferId = transferId,
            SaleAmount = 4501.65m,
            CommissionPercent = 12.5m,
            CommissionAmount = 562.71m,
            Status = CommissionStatus.Pending,
        });

        await Should.ThrowAsync<DbUpdateException>(async () =>
            await duplicate.Db.SaveChangesAsync(TestContext.Current.CancellationToken));

        await using WmsTenantScope verify = _fixture.BeginScope(tenant);
        int records = await verify.Db.CommissionRecords.CountAsync(TestContext.Current.CancellationToken);
        records.ShouldBe(1);
    }

    /// <summary>
    /// Agent orqali 10 donadan 3 tasini sotadigan, tasdiqlanmagan chiqim hujjati.
    /// </summary>
    /// <param name="scope">Tenant qamrovi.</param>
    /// <returns>Hujjat, agent va mahsulot identifikatorlari.</returns>
    /// <remarks>
    /// Narx ATAYLAB «notekis» (1500.55 × 3 × 12.5 %) — yaxlitlash yo'qolsa summa darhol farq qiladi.
    /// </remarks>
    private static async Task<(Guid TransferId, Guid AgentId, Guid ProductId)> CreateAgentSaleAsync(
        WmsTenantScope scope)
    {
        UserProfile user = await TestData.AddUserAsync(scope.Db);
        Warehouse warehouse = await TestData.AddWarehouseAsync(scope.Db);
        Product product = await TestData.AddProductAsync(scope.Db, "Pishloq 200 g");
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Do'kon №7");
        Agent agent = await TestData.AddAgentAsync(scope.Db, "Savdo agenti", commissionPercent: 10m);
        await TestData.AddStockAsync(scope.Db, warehouse, product, 10m);

        TransferDto created = await scope.Service<ITransferService>().CreateAsync(user.Id, new CreateTransferDto
        {
            Type = TransferType.Outgoing,
            FromWarehouseId = warehouse.Id,
            CounterpartyId = client.Id,
            AgentId = agent.Id,
            CommissionPercent = 12.5m,
            Items = [new CreateTransferItemDto { ProductId = product.Id, Quantity = 3m, UnitPrice = 1500.55m }],
        });

        return (created.Id, agent.Id, product.Id);
    }
}
