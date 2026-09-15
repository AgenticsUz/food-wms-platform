using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WMS.Tests.Ai.Eval;

/// <summary>
/// Sinov to'plamidagi bitta savol.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <see cref="Args"/> — FIXTURE rejimi uchun: CI'da model yo'q, shuning uchun kutilgan
/// tool O'SHA argumentlar bilan to'g'ridan-to'g'ri bajariladi va natija
/// <see cref="Markers"/> ga solishtiriladi. Ya'ni CI «WMS to'g'ri javob beradimi?» degan
/// savolga javob beradi, «model to'g'ri tool tanladimi?» degan savolga esa — jonli rejim.
/// </para>
/// <para>
/// Ikkisini ajratish shart: modelsiz birinchisini o'lchash mumkin va u har commit'da
/// yurishi kerak; ikkinchisi esa pul turadi va qo'lda yurgiziladi.
/// </para>
/// </remarks>
internal sealed class AiEvalCase
{
    public string Id { get; set; } = null!;

    /// <summary>Toifa: <c>qoldiq</c>, <c>muddat</c>, <c>qarz</c>, <c>bugungi</c>, <c>kutilayotgan</c>, <c>narx</c>, <c>noaniq</c>, <c>ruxsatsiz</c>.</summary>
    public string Category { get; set; } = null!;

    /// <summary>Savol tili: <c>uz</c>, <c>ru</c>, <c>mix</c>.</summary>
    public string Lang { get; set; } = "uz";

    /// <summary>Foydalanuvchi matni — jonli rejimda AYNAN shu yuboriladi.</summary>
    public string Text { get; set; } = null!;

    /// <summary>Savol so'ragan odamning ruxsatlari.</summary>
    public string[] Permissions { get; set; } = [];

    /// <summary>Kutilgan tool kodi.</summary>
    public string Tool { get; set; } = null!;

    /// <summary>Fixture rejimida tool'ga beriladigan argumentlar.</summary>
    public JsonElement Args { get; set; }

    /// <summary>Javobda bo'lishi SHART bo'lgan parchalar.</summary>
    public string[] Markers { get; set; } = [];

    /// <summary>Model qayta so'rashi kutiladimi (noaniq nom).</summary>
    public bool ExpectClarify { get; set; }

    /// <summary>Ruxsat yo'q — tool modelga umuman ko'rsatilmasligi kerak.</summary>
    public bool ExpectRefusal { get; set; }

    /// <summary>To'plamni o'qiydi.</summary>
    /// <returns>Savollar.</returns>
    /// <remarks>
    /// JSON manba fayl sifatida YOQILGAN (<c>WMS.Tests.csproj</c>): to'plam kodda emas,
    /// ma'lumotda yashashi kerak — yangi savol qo'shish uchun kod o'zgartirilmasin.
    /// </remarks>
    public static IReadOnlyList<AiEvalCase> Load()
    {
        string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string path = Path.Combine(directory, "Ai", "Eval", "cases.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Sinov to'plami topilmadi: {path}. `cases.json` chiqish papkasiga ko'chirilmaganmi?", path);
        }

        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        return JsonSerializer.Deserialize<List<AiEvalCase>>(File.ReadAllText(path), options)
            ?? throw new InvalidOperationException("Sinov to'plami bo'sh.");
    }
}
