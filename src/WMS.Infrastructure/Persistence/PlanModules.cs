namespace WMS.Infrastructure.Persistence;

/// <summary>
/// Plan <c>FeatureCodes</c> CSV ↔ ro'yxat.
/// </summary>
/// <remarks>
/// ⚠️ F6 da <c>ApplyPlanModulesAsync</c>/<c>ApplyAllModulesAsync</c> O'CHDI (D6): plan
/// endi modul yoqmaydi — modullar faqat Identity obunasidan (token <c>modules</c>).
/// </remarks>
public static class PlanModules
{
    public static List<string> Split(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? []
            : [.. csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    public static string Join(IEnumerable<string>? codes) =>
        codes is null
            ? ""
            : string.Join(",", codes.Select(c => c?.Trim()).Where(c => !string.IsNullOrEmpty(c)));
}
