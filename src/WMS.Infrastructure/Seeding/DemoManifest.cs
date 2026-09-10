using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using WMS.Application.Common;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Seeding;

/// <summary>
/// Demo manifesti (<c>seed/demo/tenant.json</c>) — Identity demo seed'ining WMS tomondagi ko'zgusi.
/// </summary>
/// <remarks>
/// ⚠️ Qiymatlar <c>agentics-platform/seed/demo/01-identity.sql</c> dan KO'CHIRILGAN va u bilan
/// sinxron turishi SHART (sabablari manifestning <c>_comment</c> maydonida). Manifest assemblyga
/// EmbeddedResource bo'lib kiradi — prod image'da <c>seed/</c> papkasi yo'q.
/// </remarks>
/// <param name="Version">Manifest versiyasi (faqat log uchun).</param>
/// <param name="Tenant">Identity'dagi demo tenant.</param>
/// <param name="Modules">Identity obunasidagi <c>wms</c> modullari (<c>tenant_product.modules</c>).</param>
/// <param name="PlanCode">WMS tarifi (tijorat qatlami WMS'niki — D6).</param>
/// <param name="SubscriptionStatus"><c>active</c> yoki <c>trial</c>.</param>
/// <param name="Users">Demo odamlar — har tizim roliga bittadan.</param>
public sealed partial record DemoManifest(
    int Version,
    DemoTenantSeed Tenant,
    IReadOnlyList<string> Modules,
    string PlanCode,
    string SubscriptionStatus,
    IReadOnlyList<DemoPersonSeed> Users)
{
    /// <summary>
    /// Resurs nomining oxiri. To'liq nom <c>WMS.Infrastructure.seed.demo.tenant.json</c>
    /// (<c>LinkBase="seed"</c>); oxiri bo'yicha qidiriladi — assembly nomi o'zgarsa ham topilsin.
    /// </summary>
    private const string ResourceSuffix = "seed.demo.tenant.json";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Identity'dagi tenant kodi shakli — JIT sink bilan AYNAN bir xil.</summary>
    [GeneratedRegex("^[a-z0-9][a-z0-9-]{1,31}$", RegexOptions.CultureInvariant)]
    private static partial Regex TenantCodePattern();

    [GeneratedRegex(@"^\+[1-9][0-9]{7,14}$", RegexOptions.CultureInvariant)]
    private static partial Regex E164Pattern();

    /// <summary>Manifestni o'qiydi va tekshiradi; nosozlik — istisno (fail-closed).</summary>
    public static DemoManifest Load()
    {
        Assembly assembly = typeof(DemoManifest).Assembly;
        string resource = Array.Find(
                assembly.GetManifestResourceNames(),
                r => r.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"Demo manifesti assemblyda yo'q ('{ResourceSuffix}') — seed/demo/tenant.json EmbeddedResource sifatida kirmagan.");

        using Stream stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Demo manifesti ochilmadi: {resource}");

        DemoManifest manifest = JsonSerializer.Deserialize<DemoManifest>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Demo manifesti bo'sh.");

        manifest.Validate();
        return manifest;
    }

    /// <summary>Tijorat holati. Faqat ikki qiymat: demo to'xtatilgan holatda tug'ilmasin.</summary>
    public SubscriptionStatus ParseSubscriptionStatus() => SubscriptionStatus.Trim().ToLowerInvariant() switch
    {
        "active" => Domain.Enums.SubscriptionStatus.Active,
        "trial" => Domain.Enums.SubscriptionStatus.Trial,
        _ => throw new InvalidOperationException($"Demo manifesti: subscriptionStatus '{SubscriptionStatus}' tanilmadi (active | trial)."),
    };

    /// <summary>
    /// Manifest Identity seed'idan qo'lda ko'chiriladi — xato nusxa jim o'tib ketmasin, darhol yiqilsin.
    /// </summary>
    private void Validate()
    {
        // JSON'da maydon yo'q bo'lsa STJ konstruktorga null beradi (annotatsiyaga qaramay).
        if (Tenant is null || Modules is null || Users is null || PlanCode is null || SubscriptionStatus is null)
        {
            throw new InvalidOperationException("Demo manifesti to'liq emas: tenant, modules, planCode, subscriptionStatus, users majburiy.");
        }

        if (Tenant.Id == Guid.Empty || Tenant.Code is null || !TenantCodePattern().IsMatch(Tenant.Code) || string.IsNullOrWhiteSpace(Tenant.Name))
        {
            throw new InvalidOperationException("Demo manifesti: tenant id, kod (Identity shaklida) va nom majburiy.");
        }

        // Identity obunadagi modulni katalog bilan REGISTRGA SEZGIR solishtiradi — shu yerda ham.
        string[] unknownModules = [.. Modules.Where(m => !ModuleCodes.All.Contains(m, StringComparer.Ordinal))];
        if (Modules.Count == 0 || unknownModules.Length > 0)
        {
            throw new InvalidOperationException($"Demo manifesti: modullar bo'sh yoki noma'lum ({string.Join(", ", unknownModules)}).");
        }

        _ = ParseSubscriptionStatus();

        // Demo ma'lumot har tizim roliga ishora qiladi (admin — moliya, menejer — transfer,
        // xodim — sex); viewer esa ruxsat chegarasini ko'rsatish uchun. Rol takrorlansa yoki
        // tushib qolsa ma'lumotda «egasiz» amallar paydo bo'lardi.
        if (Users.Count != WmsSystemRoles.Ordered.Count
            || !WmsSystemRoles.Ordered.All(role => Users.Count(u => string.Equals(u?.Role, role, StringComparison.Ordinal)) == 1))
        {
            throw new InvalidOperationException(
                $"Demo manifesti: har tizim roliga ({string.Join(", ", WmsSystemRoles.Ordered)}) AYNAN bitta odam kerak.");
        }

        foreach (DemoPersonSeed person in Users)
        {
            if (person.Sub == Guid.Empty || person.Phone is null || !E164Pattern().IsMatch(person.Phone) || string.IsNullOrWhiteSpace(person.FullName))
            {
                throw new InvalidOperationException($"Demo manifesti: '{person.Role}' odami uchun sub, E.164 telefon va ism majburiy.");
            }
        }

        if (Users.Select(u => u.Sub).Distinct().Count() != Users.Count)
        {
            throw new InvalidOperationException("Demo manifesti: sub'lar takrorlanmasligi kerak.");
        }
    }
}

/// <summary>Identity'dagi demo tenant.</summary>
/// <param name="Id"><c>identity.tenant.id</c> — nusxa AYNAN shu id bilan yoziladi (JIT uni id bo'yicha topadi).</param>
/// <param name="Code">Tenant kodi (tokendagi <c>tenant_code</c>).</param>
/// <param name="Name">Ko'rsatiladigan nom (tokenda yo'q — JIT o'rniga kodni yozardi).</param>
public sealed record DemoTenantSeed(Guid Id, string Code, string Name);

/// <summary>Demo odam.</summary>
/// <param name="Sub">Identity <c>user.id</c> (tokendagi <c>sub</c>).</param>
/// <param name="Phone">Telefon (E.164) — zaxira kalit, sub eskirgan holat uchun.</param>
/// <param name="FullName">Ism — Identity'dagi bilan bir xil, aks holda JIT birinchi kirishda uni qayta yozadi.</param>
/// <param name="Role">WMS tizim roli (<c>admin|manager|employee|viewer</c>) — Identity'dagi <c>wms</c> roli bilan bir xil.</param>
public sealed record DemoPersonSeed(Guid Sub, string Phone, string FullName, string Role);
