using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Common;
using WMS.Domain.Common;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Seeding;

/// <summary>Demo ma'lumotda ishtirok etadigan profillar (har tizim roliga bittadan).</summary>
internal sealed record DemoPeople(UserProfile Admin, UserProfile Manager, UserProfile Employee, UserProfile Viewer);

/// <summary>
/// Demo muzqaymoq zavodining ma'lumoti va 35 kunlik ish tarixi (eski <c>SeedDemoDataAsync</c> to'plami).
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>Raqamlar ICHKI MOS bo'lishi shart</b> — demo'da «zaxira 285, lekin partiyalar yig'indisi
/// 300» degan ekran ishonchni birinchi daqiqada o'ldiradi. Shuning uchun har harakat servislardagi
/// qoida bilan XOTIRADAGI daftar orqali o'tadi: kirim partiya+qoldiq ochadi, sotuv va ishlab
/// chiqarish FEFO bilan qoldiqni ham, partiya qoldig'ini ham kamaytiradi, qarz faqat
/// <see cref="ChangeDebt"/> orqali o'zgaradi. Oxirida <see cref="VerifyLedger"/> bularni qayta
/// hisoblab solishtiradi.
/// </para>
/// <para>
/// Eski to'plamdagi xatolar tuzatildi: xom ashyo ishlatilgan kundan KEYIN kelardi; tayyor mahsulot
/// «hech qayerdan» 5000 donalik partiya bilan paydo bo'lardi; mijozlar sotuvdan ko'p to'lab qarz
/// manfiy chiqardi. Endi tayyor mahsulot faqat yakunlangan buyurtmadan keladi va sanalar ketma-ket.
/// </para>
/// </remarks>
internal sealed partial class DemoData
{
    /// <summary>
    /// Toshkent UTC+5 (yozgi vaqt yo'q). Smena va ish soatlari mahalliy vaqtda o'ylangan, bazada UTC —
    /// usiz «08:00 da boshlangan buyurtma» ekranda 13:00 bo'lib chiqardi.
    /// </summary>
    private static readonly TimeSpan TashkentOffset = TimeSpan.FromHours(5);

    private readonly WmsDbContext _db;
    private readonly DemoPeople _people;
    private readonly DateTime _now;
    private readonly DateTime _localToday;

    /// <summary>Katalog «yaratilgan» payt — tarixdagi birinchi voqeadan oldin.</summary>
    private readonly DateTime _start;

    /// <summary>Yaratilgan har yozuv va uning TARIXIY vaqti (orqaga sanalash uchun).</summary>
    private readonly List<(BaseEntity Entity, DateTime At)> _created = [];

    private readonly Dictionary<Guid, Unit> _unitsById = [];
    private readonly Dictionary<Guid, Product> _productsById = [];

    public DemoData(WmsDbContext db, DemoPeople people, DateTime utcNow)
    {
        _db = db;
        _people = people;
        _now = utcNow;
        _localToday = (utcNow + TashkentOffset).Date;
        _start = Day(35, 9);
    }

    /// <returns>Tur bo'yicha yaratilgan yozuvlar soni (log uchun).</returns>
    public async Task<IReadOnlyDictionary<string, int>> WriteAsync(CancellationToken ct)
    {
        Dictionary<string, Unit> units = (await _db.Units.ToListAsync(ct))
            .GroupBy(u => u.ShortName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        BuildCatalog(units);
        BuildPartners();
        BuildProductionSetup();
        BuildQualitySetup();
        BuildShifts();
        BuildFleet();

        PlayHistory();

        BuildShiftKpi();
        BuildAttendance();

        VerifyLedger();

        await _db.SaveChangesAsync(ct);

        // ⚠️ Ikkinchi o'tish: StampEntries yangi yozuvga CreatedAt = HOZIR qo'yadi, ya'ni 35 kunlik
        // tarix bir soniyada «yaratilgan» bo'lib ro'yxatlar tartibi va «oxirgi 7 kun» filtrlari
        // buzilardi. O'zgartirilgan (Modified) yozuvda StampEntries CreatedAt'ga tegmaydi —
        // tarixiy vaqt shu yo'l bilan qoladi (UpdatedAt esa seed paytini ko'rsatadi, bu to'g'ri).
        foreach ((BaseEntity entity, DateTime at) in _created)
        {
            entity.CreatedAt = at;
        }

        await _db.SaveChangesAsync(ct);

        return _created
            .GroupBy(c => c.Entity.GetType().Name, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
    }

    /// <summary>
    /// Tarix — XRONOLOGIK tartibda (FEFO to'g'ri partiyani olsin: har chaqiruv o'z vaqtidagi qoldiqni ko'radi).
    /// </summary>
    private void PlayHistory()
    {
        // Doimiy xarajatlar (kassa daftari) — oy boshidan.
        Expense("Ishchilar oyligi", 3_500_000, Day(30, 12));
        Expense("Elektr energiya", 850_000, Day(25, 12));
        Expense("Qadoqlash materiallari", 1_200_000, Day(18, 12));
        Expense("Transport xarajatlari", 450_000, Day(12, 12));
        Expense("Kommunal xizmatlar", 380_000, Day(6, 12));

        // −28…−22: xom ashyo kirimi. Eski to'plamda kirim −25…−3 kunda, ishlatish esa −20 kunda
        // edi (saryog' kelmasidan besh kun oldin ishlatilgan) — sanalar ishlab chiqarishdan oldinga surildi.
        Transfer inMilkPowder = Incoming(_nemat, _quruqSut, 500, 25_000, Day(28, 9), _locA1);
        Transfer inCondensed = Incoming(_baraka, _quyuqSut, 200, 8_000, Day(27, 10), _locA1);
        Check(null, inCondensed, _qcTaste, "Yangi, begona hidsiz", true, Day(27, 11));
        Incoming(_sharq, _shakar, 300, 12_000, Day(26, 9), _locA2);
        Transfer inButter = Incoming(_nemat, _saryog, 50, 45_000, Day(25, 11), _locA1);
        Incoming(_nemat, _stabilizator, 15, 95_000, Day(24, 9), _locA2);
        Incoming(_sharq, _kokos, 30, 38_000, Day(22, 10), _locA2);

        Payment(_nemat, PaymentDirection.Out, 12_500_000, PaymentMethod.Bank, Day(20, 15), inMilkPowder);

        DemoOrder o1 = CompletedOrder(_plombirRecipe, 1000, Day(20, 8), Day(18, 17), actual: 980, waste: 20, createdAt: Day(21, 16));
        Check(o1.Stages[0], null, _qcFat, "12.5", true, o1.Stages[0].EndTime!.Value);
        Check(o1.Stages[0], null, _qcSolids, "38.4", true, o1.Stages[0].EndTime!.Value);
        Check(o1.Stages[2], null, _qcTemperature, "-20", true, o1.Stages[2].EndTime!.Value);
        Check(o1.Stages[4], null, _qcAppearance, "true", true, o1.Stages[4].EndTime!.Value);
        Check(o1.Stages[4], null, _qcTaste, "Me'yorida", true, o1.Stages[4].EndTime!.Value);

        Payment(_baraka, PaymentDirection.Out, 1_600_000, PaymentMethod.Bank, Day(18, 11), inCondensed);

        Transfer s1 = Sale(_korzinka, _plombir, 500, 5_000, Day(17, 10), _anvar);
        LowStockAlert(_plombir, Day(17, 10));

        // Aksiyadan qolgan qism qaytadi — partiya asl sanalarini saqlaydi (servis qoidasi) va keyingi sotuvda FEFO uni oladi.
        Return(s1, _plombir, 20, ReturnReason.Unsold, Day(15, 14), "Aksiya tugagach sotilmay qolgan qism qaytarildi");

        DemoOrder o4 = CompletedOrder(_shokoladRecipe, 500, Day(15, 8), Day(13, 17), actual: 490, waste: 10, createdAt: Day(16, 15));
        Check(o4.Stages[2], null, _qcTemperature, "-21", true, o4.Stages[2].EndTime!.Value);

        Sale(_makro, _plombir, 300, 5_000, Day(14, 10), _sevara, out Transfer s2);
        LowStockAlert(_plombir, Day(14, 10));

        DemoOrder o5 = CompletedOrder(_kremRecipe, 400, Day(12, 8), Day(10, 17), actual: 392, waste: 8, createdAt: Day(13, 15));
        Check(o5.Stages[0], null, _qcFat, "15.2", true, o5.Stages[0].EndTime!.Value);

        Payment(_nemat, PaymentDirection.Out, 2_250_000, PaymentMethod.Bank, Day(12, 15), inButter);

        Transfer s3 = Sale(_korzinka, _shokolad, 200, 6_000, Day(12, 10), _anvar);
        LowStockAlert(_shokolad, Day(12, 10));

        // Ikkinchi quruq sut partiyasi — FEFO namoyishi: ishlab chiqarish avval eskisini oladi.
        Transfer inMilkPowder2 = Incoming(_baraka, _quruqSut, 300, 25_000, Day(10, 9), _locA1);
        Check(null, inMilkPowder2, _qcAppearance, "true", true, Day(10, 10));

        Payment(_korzinka, PaymentDirection.In, 2_400_000, PaymentMethod.Bank, Day(10, 16), s1);

        DemoOrder o2 = CompletedOrder(_plombirRecipe, 500, Day(10, 8), Day(8, 17), actual: 485, waste: 15, createdAt: Day(11, 15));
        Check(o2.Stages[2], null, _qcTemperature, "-16", false, o2.Stages[2].EndTime!.Value,
            "Sovutish kamerasi harorati ko'tarilgan — kamera sozlandi, partiya qayta sovutildi");
        Check(o2.Stages[2], null, _qcTemperature, "-19", true, o2.Stages[2].EndTime!.Value.AddHours(1), "Qayta tekshiruv");

        Payment(_makro, PaymentDirection.In, 1_500_000, PaymentMethod.Bank, Day(9, 12), s2);

        Transfer s4 = Sale(_plov, _kremBrule, 150, 5_500, Day(8, 10), agent: null);
        LowStockAlert(_kremBrule, Day(8, 10));

        PayCommissions(_anvar, Day(7, 17));

        Payment(_plov, PaymentDirection.In, 825_000, PaymentMethod.Cash, Day(6, 13), s4);

        // Qisman to'lov — S3 komissiyasi «kutilmoqda» holatida qoladi (mijoz to'liq to'lamagan).
        Payment(_korzinka, PaymentDirection.In, 700_000, PaymentMethod.Bank, Day(5, 11), s3);
        Payment(_sharq, PaymentDirection.Out, 4_740_000, PaymentMethod.Bank, Day(5, 15));

        // Uch partiyadan yig'iladi: O1 qoldig'i, qaytgan partiya, O2 — FEFO ekranda ko'rinsin.
        Sale(_makro, _plombir, 400, 5_000, Day(4, 10), _sevara, out Transfer s5);
        LowStockAlert(_plombir, Day(4, 10));

        DemoOrder o3 = InProgressOrder(_plombirRecipe, 300, Day(2, 8), completedStages: 2, actual: 298, waste: 2, createdAt: Day(3, 16));
        Check(o3.Stages[0], null, _qcFat, "17.1", false, o3.Stages[0].EndTime!.Value,
            "Saryog' me'yordan ortiq — keyingi aralashmada kamaytiriladi");
        Notify(null, "Sifat nazorati: me'yordan chetlanish",
            $"{_plombirRecipe.Recipe.Name}: yog'lilik 17.1% (me'yor 10–16%). Texnolog tekshiruvi kerak.",
            NotificationType.Warning, "ProductionOrder", o3.Order.Id, o3.Stages[0].EndTime!.Value);

        Payment(_makro, PaymentDirection.In, 1_000_000, PaymentMethod.Bank, Day(2, 12));

        // Tasdiq kutayotgan kirim — «Tasdiqlash» oqimini demo'da ko'rsatish uchun (zaxira/qarzga ta'siri yo'q).
        PendingIncoming(_nemat, _stabilizator, 10, 95_000, Day(1, 16));

        DraftOrder(_kremRecipe, 600, Day(-2, 8), Day(-4, 17), createdAt: Day(1, 11));

        BuildDeliveries(s1, s2, s3, s4, s5);
    }

    // ── Umumiy yordamchilar ──

    /// <summary><paramref name="daysAgo"/> kun oldin, mahalliy <paramref name="hour"/>:<paramref name="minute"/> — UTC da.</summary>
    private DateTime Day(int daysAgo, int hour, int minute = 0) =>
        _localToday.AddDays(-daysAgo).AddHours(hour).AddMinutes(minute) - TashkentOffset;

    /// <summary>Kelajakdagi vaqtni hozirga qisadi — «bugun 10:00 da yetkazildi» seed ertalab yurganda kelajak bo'lmasin.</summary>
    private DateTime Past(DateTime at) => at > _now ? _now : at;

    private T Add<T>(T entity, DateTime? at = null)
        where T : BaseEntity
    {
        // Qidiruv ustuni (P2.1) — YAGONA joyda: demo yuzlab qator yasaydi va har
        // chaqiruvda `NameSearch` yozib o'tirish bittasini unutish bilan tugardi.
        switch (entity)
        {
            case Product product:
                product.NameSearch = SearchNormalizer.Normalize(product.Name);
                break;
            case Counterparty counterparty:
                counterparty.NameSearch = SearchNormalizer.Normalize(counterparty.Name);
                break;
            case Warehouse warehouse:
                warehouse.NameSearch = SearchNormalizer.Normalize(warehouse.Name);
                break;
            default:
                break;
        }

        _db.Add(entity);
        _created.Add((entity, at ?? _start));
        return entity;
    }

    private IEnumerable<T> Created<T>() => _created.Select(c => c.Entity).OfType<T>();

    private static string Fmt(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private string UnitOf(Product product) => _unitsById[product.UnitId].ShortName;
}
