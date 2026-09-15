using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.Interfaces;

namespace WMS.Infrastructure.Services.Ai.Tools;

/// <summary>Nom bo'yicha qidiruv argumentlari.</summary>
public sealed class FindByNameArgs
{
    /// <summary>Foydalanuvchi aytgan nom (lotin, kirill, xato yozilgan — farqi yo'q).</summary>
    public string? Name { get; set; }
}

/// <summary>
/// <c>find_product</c> — nom bo'yicha mahsulot nomzodlari.
/// </summary>
/// <remarks>
/// Modelga ID'lar ATAYLAB ko'rsatiladi: qolgan tool'lar mahsulotni nom bo'yicha qayta
/// izlamasin. Ikki marta izlash bir xil noaniqlikni ikki marta hal qilishni talab qilardi
/// va ikkinchisida boshqacha javob chiqishi mumkin edi.
/// </remarks>
public sealed class FindProductTool : AiTool<FindByNameArgs>
{
    private readonly ISearchService _search;

    public FindProductTool(ISearchService search) => _search = search;

    public override string Code => "find_product";

    public override string Description =>
        "Mahsulotni nomi bo'yicha topadi. Foydalanuvchi mahsulot nomini aytganda, boshqa "
        + "tool'dan OLDIN shuni chaqiring: natijadagi id qolgan tool'larga beriladi. "
        + "Bir nechta nomzod qaytsa — qaysi biri kerakligini foydalanuvchidan so'rang.";

    public override string PermissionCode => WmsPermissions.ProductsView;

    protected override async Task<AiToolResult> RunAsync(
        FindByNameArgs args, AiToolContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(args.Name))
        {
            return AiToolResult.Fail("'name' bo'sh — qidiriladigan nomni bering.");
        }

        IReadOnlyList<ProductSearchCandidate> candidates =
            await _search.FindProductsAsync(args.Name, AiNameResolver.MaxCandidates, cancellationToken);

        if (candidates.Count == 0)
        {
            return new AiToolResult(AiNameResolver.NotFoundText(args.Name, "mahsulot"), Array.Empty<object>());
        }

        string text = string.Join("\n", candidates.Select(Describe));
        return new AiToolResult($"{candidates.Count} ta mahsulot:\n{text}", candidates);
    }

    private static string Describe(ProductSearchCandidate c) =>
        c.Barcode is { Length: > 0 }
            ? $"- {c.Name} (id: {c.Id}, shtrix-kod: {c.Barcode})"
            : $"- {c.Name} (id: {c.Id})";
}

/// <summary>
/// <c>find_counterparty</c> — nom bo'yicha mijoz/ta'minotchi nomzodlari.
/// </summary>
/// <remarks>
/// Tur (mijoz/ta'minotchi) natijada KO'RSATILADI: bir xil nom ikki rolda bo'lishi mumkin
/// va «Korzinka»ning qarzi qaysi tomonga ekani shunga bog'liq.
/// </remarks>
public sealed class FindCounterpartyTool : AiTool<FindByNameArgs>
{
    private readonly ISearchService _search;

    public FindCounterpartyTool(ISearchService search) => _search = search;

    public override string Code => "find_counterparty";

    public override string Description =>
        "Mijoz yoki ta'minotchini nomi bo'yicha topadi. Foydalanuvchi kontragent nomini "
        + "aytganda, boshqa tool'dan OLDIN shuni chaqiring. Bir nechta nomzod qaytsa — "
        + "qaysi biri kerakligini foydalanuvchidan so'rang.";

    public override string PermissionCode => WmsPermissions.PartnersView;

    protected override async Task<AiToolResult> RunAsync(
        FindByNameArgs args, AiToolContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(args.Name))
        {
            return AiToolResult.Fail("'name' bo'sh — qidiriladigan nomni bering.");
        }

        IReadOnlyList<CounterpartySearchCandidate> candidates =
            await _search.FindCounterpartiesAsync(args.Name, AiNameResolver.MaxCandidates, cancellationToken);

        if (candidates.Count == 0)
        {
            return new AiToolResult(AiNameResolver.NotFoundText(args.Name, "kontragent"), Array.Empty<object>());
        }

        string text = string.Join("\n", candidates.Select(c => $"- {c.Name} ({c.Type}, id: {c.Id})"));
        return new AiToolResult($"{candidates.Count} ta kontragent:\n{text}", candidates);
    }
}
