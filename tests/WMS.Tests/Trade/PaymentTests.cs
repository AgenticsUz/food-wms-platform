using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Application.DTOs.Finance;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Tests.Infrastructure;

namespace WMS.Tests.Trade;

/// <summary>
/// Kontragent bilan pul oldi-berdisi: yo'nalish, hujjat sanasi va storno (P2.8, P2.9).
/// </summary>
/// <remarks>
/// <para>
/// Nima uchun kerak. To'lov — qarz balansini O'ZGARTIRADIGAN yagona qo'lda yozuv, ya'ni
/// xatosi to'g'ridan-to'g'ri pulda ko'rinadi. Uchta da'vo qo'riqlanadi:
/// </para>
/// <list type="number">
/// <item><b>Yo'nalish taxmin qilinmaydi</b>: balans nol bo'lganda servis so'raydi. Ilgari
/// u belgiga qarab tanlanardi va nol balansda summa teskari tomonga yozilib, xatoni ikki
/// barobar qilardi (AI qatlami uchun bu ayniqsa xavfli — F10·A3.2).</item>
/// <item><b>Hujjat sanasi</b>: «kecha to'lagan edi» yozilishi kerak, lekin faqat
/// <c>documents.backdate</c> ruxsati bilan; kelajak sana hech kimga.</item>
/// <item><b>Storno</b>: xato to'lov O'CHIRILMAYDI, teskari yozuv bilan qaytariladi va
/// ikki marta qaytarib bo'lmaydi. Ilgari to'lovni tuzatishning YO'LI umuman yo'q edi.</item>
/// </list>
/// </remarks>
[Collection(WmsDatabaseCollection.Name)]
public sealed class PaymentTests
{
    private readonly WmsDatabaseFixture _fixture;

    /// <summary>xUnit fixture'ni beradi.</summary>
    /// <param name="fixture">Baza fixture'i.</param>
    public PaymentTests(WmsDatabaseFixture fixture) => _fixture = fixture;

    private Task<TestTenant> NewTenantAsync() =>
        _fixture.CreateTenantAsync($"t-{Guid.NewGuid().ToString("N")[..12]}");

    [Fact]
    public async Task Mijoz_tolovi_qarzni_kamaytiradi_va_yonalish_saqlanadi()
    {
        TestTenant tenant = await NewTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        UserProfile user = await TestData.AddUserAsync(scope.Db);
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Korzinka");
        scope.AsUser(user.Id, WmsPermissions.FinanceManage);

        // Mijoz bizdan qarzdor: 5 000 000.
        scope.Db.Debts.Add(new Debt { CounterpartyId = client.Id, Amount = 5_000_000m });
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        PaymentHistoryDto payment = await scope.Service<IFinanceService>().CreatePaymentAsync(user.Id,
            new CreatePaymentDto
            {
                CounterpartyId = client.Id,
                Amount = 2_000_000m,
                Method = PaymentMethod.Cash,
                Direction = PaymentDirection.In,
            });

        payment.Direction.ShouldBe(PaymentDirection.In);
        payment.Source.ShouldBe(DocumentSource.Ui);
        payment.DocumentDate.Date.ShouldBe(DateTime.UtcNow.Date);
        payment.IsReversed.ShouldBeFalse();

        decimal debt = await scope.Db.Debts.Where(d => d.CounterpartyId == client.Id)
            .Select(d => d.Amount).SingleAsync(TestContext.Current.CancellationToken);
        debt.ShouldBe(3_000_000m);
    }

    [Fact]
    public async Task Taminotchiga_tolov_qarzni_oshiradi()
    {
        TestTenant tenant = await NewTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        UserProfile user = await TestData.AddUserAsync(scope.Db);
        Counterparty supplier = await TestData.AddCounterpartyAsync(scope.Db, "Sut zavodi", CounterpartyType.Supplier);
        scope.AsUser(user.Id, WmsPermissions.FinanceManage);

        // Biz ta'minotchiga qarzdormiz: balans manfiy.
        scope.Db.Debts.Add(new Debt { CounterpartyId = supplier.Id, Amount = -4_000_000m });
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        PaymentHistoryDto payment = await scope.Service<IFinanceService>().CreatePaymentAsync(user.Id,
            new CreatePaymentDto
            {
                CounterpartyId = supplier.Id,
                Amount = 1_500_000m,
                Method = PaymentMethod.Bank,
                Direction = PaymentDirection.Out,
            });

        payment.Direction.ShouldBe(PaymentDirection.Out);

        decimal debt = await scope.Db.Debts.Where(d => d.CounterpartyId == supplier.Id)
            .Select(d => d.Amount).SingleAsync(TestContext.Current.CancellationToken);
        debt.ShouldBe(-2_500_000m);   // qarzimiz kamaydi
    }

    [Fact]
    public async Task Nol_balansda_yonalish_taxmin_QILINMAYDI()
    {
        TestTenant tenant = await NewTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        UserProfile user = await TestData.AddUserAsync(scope.Db);
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Yangi mijoz");
        scope.AsUser(user.Id, WmsPermissions.FinanceManage);

        // DARVOZA: yo'nalish berilmagan va balans nol — servis so'rashi SHART.
        AppException error = await Should.ThrowAsync<AppException>(async () =>
            await scope.Service<IFinanceService>().CreatePaymentAsync(user.Id, new CreatePaymentDto
            {
                CounterpartyId = client.Id,
                Amount = 100_000m,
                Method = PaymentMethod.Cash,
            }));

        error.MessageTemplate.ShouldBe("Payment direction is required when the balance is zero");

        (await scope.Db.PaymentHistories.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task Kelajak_sana_hech_kimga_ruxsat_emas()
    {
        TestTenant tenant = await NewTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        UserProfile user = await TestData.AddUserAsync(scope.Db);
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Mijoz");

        // Orqaga sana ruxsati BOR foydalanuvchi ham kelajakka yoza olmaydi.
        scope.AsUser(user.Id, WmsPermissions.FinanceManage, WmsPermissions.DocumentsBackdate);

        AppException error = await Should.ThrowAsync<AppException>(async () =>
            await scope.Service<IFinanceService>().CreatePaymentAsync(user.Id, new CreatePaymentDto
            {
                CounterpartyId = client.Id,
                Amount = 50_000m,
                Method = PaymentMethod.Cash,
                Direction = PaymentDirection.In,
                DocumentDate = DateTime.UtcNow.AddDays(1),
            }));

        error.MessageTemplate.ShouldBe(DocumentDates.FutureMessage);
    }

    [Fact]
    public async Task Orqaga_sana_faqat_ruxsat_bilan_yoziladi()
    {
        TestTenant tenant = await NewTenantAsync();
        Guid clientId;
        Guid userId;

        await using (WmsTenantScope scope = _fixture.BeginScope(tenant))
        {
            UserProfile user = await TestData.AddUserAsync(scope.Db);
            Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Mijoz");
            userId = user.Id;
            clientId = client.Id;

            // Ruxsatsiz: 403.
            scope.AsUser(user.Id, WmsPermissions.FinanceManage);
            await Should.ThrowAsync<ForbiddenException>(async () =>
                await scope.Service<IFinanceService>().CreatePaymentAsync(user.Id, new CreatePaymentDto
                {
                    CounterpartyId = client.Id,
                    Amount = 10_000m,
                    Method = PaymentMethod.Cash,
                    Direction = PaymentDirection.In,
                    DocumentDate = DateTime.UtcNow.AddDays(-1),
                }));
        }

        await using WmsTenantScope allowed = _fixture.BeginScope(tenant);
        allowed.AsUser(userId, WmsPermissions.FinanceManage, WmsPermissions.DocumentsBackdate);

        DateTime yesterday = DateTime.UtcNow.Date.AddDays(-1);
        PaymentHistoryDto payment = await allowed.Service<IFinanceService>().CreatePaymentAsync(userId,
            new CreatePaymentDto
            {
                CounterpartyId = clientId,
                Amount = 10_000m,
                Method = PaymentMethod.Cash,
                Direction = PaymentDirection.In,
                DocumentDate = yesterday,
            });

        payment.DocumentDate.ShouldBe(yesterday);

        // `PaidAt` — audit izi, u DOIM bugun (sana bilan aralashmaydi).
        payment.PaidAt.Date.ShouldBe(DateTime.UtcNow.Date);
    }

    [Fact]
    public async Task Xato_tolov_qaytarilsa_qarz_avvalgi_qiymatiga_qaytadi()
    {
        TestTenant tenant = await NewTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        UserProfile user = await TestData.AddUserAsync(scope.Db);
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Korzinka");
        scope.AsUser(user.Id, WmsPermissions.FinanceManage);

        scope.Db.Debts.Add(new Debt { CounterpartyId = client.Id, Amount = 5_000_000m });
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        IFinanceService finance = scope.Service<IFinanceService>();
        PaymentHistoryDto wrong = await finance.CreatePaymentAsync(user.Id, new CreatePaymentDto
        {
            CounterpartyId = client.Id,
            Amount = 2_000_000m,
            Method = PaymentMethod.Cash,
            Direction = PaymentDirection.In,
        });

        PaymentHistoryDto reversal = await finance.ReversePaymentAsync(user.Id, wrong.Id, "Summa xato kiritilgan");

        reversal.ReversalOfId.ShouldBe(wrong.Id);
        reversal.Direction.ShouldBe(PaymentDirection.Out);
        reversal.Amount.ShouldBe(2_000_000m);
        reversal.ReversalReason.ShouldBe("Summa xato kiritilgan");

        decimal debt = await scope.Db.Debts.Where(d => d.CounterpartyId == client.Id)
            .Select(d => d.Amount).SingleAsync(TestContext.Current.CancellationToken);
        debt.ShouldBe(5_000_000m);   // avvalgi holat

        // Asl yozuv TARIXDA qoladi va «qaytarilgan» deb belgilanadi.
        List<PaymentHistoryDto> history = await finance.GetPaymentsAsync(client.Id, 1, 50);
        history.Count.ShouldBe(2);
        history.Single(p => p.Id == wrong.Id).IsReversed.ShouldBeTrue();
    }

    [Fact]
    public async Task Bir_tolov_ikki_marta_qaytarilmaydi()
    {
        TestTenant tenant = await NewTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        UserProfile user = await TestData.AddUserAsync(scope.Db);
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Mijoz");
        scope.AsUser(user.Id, WmsPermissions.FinanceManage);

        scope.Db.Debts.Add(new Debt { CounterpartyId = client.Id, Amount = 1_000_000m });
        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        IFinanceService finance = scope.Service<IFinanceService>();
        PaymentHistoryDto payment = await finance.CreatePaymentAsync(user.Id, new CreatePaymentDto
        {
            CounterpartyId = client.Id,
            Amount = 300_000m,
            Method = PaymentMethod.Cash,
            Direction = PaymentDirection.In,
        });

        PaymentHistoryDto reversal = await finance.ReversePaymentAsync(user.Id, payment.Id, "Xato");

        AppException second = await Should.ThrowAsync<AppException>(async () =>
            await finance.ReversePaymentAsync(user.Id, payment.Id, "Yana bir marta"));
        second.MessageTemplate.ShouldBe("This payment is already reversed");

        // Stornoning o'zini ham qaytarib bo'lmaydi.
        AppException chained = await Should.ThrowAsync<AppException>(async () =>
            await finance.ReversePaymentAsync(user.Id, reversal.Id, "Zanjir"));
        chained.MessageTemplate.ShouldBe("A reversal cannot be reversed");

        decimal debt = await scope.Db.Debts.Where(d => d.CounterpartyId == client.Id)
            .Select(d => d.Amount).SingleAsync(TestContext.Current.CancellationToken);
        debt.ShouldBe(1_000_000m);
    }

    [Fact]
    public async Task AI_kiritgan_tolov_manbasi_bilan_yoziladi()
    {
        TestTenant tenant = await NewTenantAsync();
        await using WmsTenantScope scope = _fixture.BeginScope(tenant);

        UserProfile user = await TestData.AddUserAsync(scope.Db);
        Counterparty client = await TestData.AddCounterpartyAsync(scope.Db, "Mijoz");
        scope.AsUser(user.Id, WmsPermissions.FinanceManage);

        // ⚠️ Manba DTO'dan EMAS, chaqiruvchidan keladi — mijoz o'zini «AI» deb ko'rsatolmaydi.
        PaymentHistoryDto payment = await scope.Service<IFinanceService>().CreatePaymentAsync(user.Id,
            new CreatePaymentDto
            {
                CounterpartyId = client.Id,
                Amount = 750_000m,
                Method = PaymentMethod.Cash,
                Direction = PaymentDirection.In,
            },
            DocumentSource.Ai);

        payment.Source.ShouldBe(DocumentSource.Ai);
    }
}
