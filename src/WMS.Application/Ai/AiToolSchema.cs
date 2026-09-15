using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace WMS.Application.Ai;

/// <summary>
/// Tool argumentlari sxemasini DTO turidan quradi.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ Sxema QO'LDA yozilmaydi: qo'lda yozilgan sxema DTO o'zgarganda jimgina eskirardi va
/// model mavjud bo'lmagan maydonni to'ldirib yuborardi. <c>JsonSchemaExporter</c> uni
/// turdan chiqaradi, ya'ni DTO'ga maydon qo'shilishi sxemani AVTOMAT yangilaydi.
/// </para>
/// <para>
/// Nomlash <c>JsonSerializerDefaults.Web</c> (camelCase) — argumentlar o'sha sozlamalar
/// bilan deserializatsiya qilinadi; ikki xil nomlash sxemani yolg'on qilardi.
/// </para>
/// </remarks>
public static class AiToolSchema
{
    /// <summary>Tool argumentlari uchun JSON sozlamalari — sxema ham, o'qish ham SHU bilan.</summary>
    /// <remarks>
    /// ⚠️ <see cref="JsonSerializerOptions.TypeInfoResolver"/> OSHKORA berilgan. Sukut
    /// bo'yicha u <see langword="null"/> va birinchi (de)serializatsiyada o'z-o'zidan
    /// to'ldiriladi — lekin <c>JsonSchemaExporter</c> sozlamalarni faqat O'QISHGA
    /// belgilaydi va resolver hali yo'q bo'lsa «read-only» xatosi bilan yiqiladi.
    /// Ya'ni xatti-harakat TARTIBGA bog'liq edi: agar biror tool avval bajarilgan
    /// bo'lsa sxema qurilardi, gateway esa sxemani BIRINCHI so'raydi — demak prod'da
    /// AI birinchi savoldayoq yiqilardi (jonli sinov to'plami shuni ushladi).
    /// </remarks>
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),

        // Model enum'ni nomi bilan yozadi (`"outgoing"`), raqam bilan emas.
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// Sxema eksporti sozlamalari.
    /// </summary>
    /// <remarks>
    /// ⚠️ <c>TreatNullObliviousAsNonNullable</c> — SHART. Sukut bo'yicha eksporter
    /// nullability annotatsiyasi yo'q har havola turini «bo'lishi ham mumkin, bo'lmasligi
    /// ham» deb belgilaydi va sxemaga <c>"type": ["string", "null"]</c> yozadi — ildiz
    /// obyektning O'ZI ham shunday bo'lardi. Tool argumenti hech qachon <c>null</c> emas,
    /// shuning uchun ortiqcha variant faqat modelni chalg'itadi.
    /// </remarks>
    private static readonly JsonSchemaExporterOptions ExporterOptions = new()
    {
        TreatNullObliviousAsNonNullable = true,
    };

    /// <summary>Argumentsiz tool sxemasi.</summary>
    public static JsonElement Empty { get; } = Parse("""{"type":"object","properties":{},"additionalProperties":false}""");

    /// <summary>DTO turidan sxema.</summary>
    /// <typeparam name="T">Argument DTO'si.</typeparam>
    /// <returns>JSON Schema (<c>type: object</c>).</returns>
    public static JsonElement For<T>()
    {
        JsonNode node = JsonSchemaExporter.GetJsonSchemaAsNode(SerializerOptions, typeof(T), ExporterOptions);

        // Ildiz `object` bo'lishi SHART: provayder tool sxemasi sifatida boshqa turni
        // qabul qilmaydi va xato faqat birinchi jonli chaqiriqda ko'rinardi.
        if (node is not JsonObject root || root["type"] is not JsonValue type || type.GetValue<string>() != "object")
        {
            throw new InvalidOperationException(
                $"'{typeof(T).Name}' tool argumenti bo'la olmaydi: ildiz sxemasi 'object' emas.");
        }

        return Parse(root.ToJsonString());
    }

    /// <summary>
    /// Argumentlarni DTO'ga o'qiydi.
    /// </summary>
    /// <typeparam name="T">Argument DTO'si.</typeparam>
    /// <param name="arguments">Model yuborgan JSON.</param>
    /// <param name="value">O'qilgan DTO; bo'sh argument — sukut qiymatlari bilan.</param>
    /// <param name="error">Sxemaga mos kelmasa — modelga qaytariladigan sabab.</param>
    /// <returns>O'qildimi.</returns>
    /// <remarks>
    /// ⚠️ Istisno ATAYLAB ko'tarilmaydi: sxemani buzib yuborilgan argument — KUTILGAN holat,
    /// nosozlik emas. Sabab tool natijasi bo'lib modelga qaytadi va u o'zini tuzatib qayta
    /// chaqiradi; istisno bo'lsa butun suhbat 500 bilan uzilardi.
    /// </remarks>
    public static bool TryRead<T>(JsonElement arguments, out T value, out string? error)
        where T : new()
    {
        error = null;

        if (arguments.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            value = new T();
            return true;
        }

        try
        {
            value = arguments.Deserialize<T>(SerializerOptions) ?? new T();
            return true;
        }
        catch (JsonException ex)
        {
            value = new T();
            error = ex.Message;
            return false;
        }
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
