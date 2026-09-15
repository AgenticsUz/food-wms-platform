using System.Text.Json;

namespace WMS.Application.Ai;

/// <summary>Suhbatdagi xabar egasi.</summary>
/// <remarks>
/// <c>System</c> roli ATAYLAB yo'q: system prompt xabarlar oqimida emas, <see cref="LlmRequest"/>
/// ning alohida maydonlarida turadi — u keshlanadigan prefiksning bir qismi va uni xabarlar
/// ro'yxatiga qo'shish keshni har suhbatda buzardi.
/// </remarks>
public enum LlmRole
{
    User = 1,
    Assistant = 2,
}

/// <summary>Model chaqirmoqchi bo'lgan tool.</summary>
/// <param name="Id">Chaqiriq identifikatori — natija AYNAN shu id bilan qaytariladi.</param>
/// <param name="Name">Tool kodi (<c>stock_query</c>).</param>
/// <param name="Arguments">Argumentlar — tool JSON sxemasiga mos obyekt.</param>
public sealed record LlmToolCall(string Id, string Name, JsonElement Arguments);

/// <summary>Bajarilgan tool natijasi.</summary>
/// <param name="ToolCallId"><see cref="LlmToolCall.Id"/> — mos kelmasa provayder so'rovni rad etadi.</param>
/// <param name="Content">Natija matni (odatda JSON).</param>
/// <param name="IsError">
/// Tool xato bergan. Xato ham NATIJA sifatida qaytariladi (istisno sifatida emas):
/// model xatoni ko'rib qayta so'rashi yoki boshqa yo'l tanlashi kerak, aks holda sikl uziladi.
/// </param>
public sealed record LlmToolResult(string ToolCallId, string Content, bool IsError = false);

/// <summary>Suhbatdagi bitta xabar.</summary>
/// <remarks>
/// Bitta yozuvda ham matn, ham tool chaqiriqlari bo'lishi mumkin (model tool chaqirishdan oldin
/// bir gap yozadi). Tool natijalari esa — KEYINGI, <see cref="LlmRole.User"/> xabari: Anthropic
/// shakli shunday va uni buzish javobni rad ettiradi.
/// </remarks>
public sealed record LlmMessage
{
    public required LlmRole Role { get; init; }

    public string? Text { get; init; }

    public IReadOnlyList<LlmToolCall> ToolCalls { get; init; } = [];

    public IReadOnlyList<LlmToolResult> ToolResults { get; init; } = [];

    public static LlmMessage FromUser(string text) =>
        new() { Role = LlmRole.User, Text = text };

    public static LlmMessage FromAssistant(string? text, IReadOnlyList<LlmToolCall>? toolCalls = null) =>
        new() { Role = LlmRole.Assistant, Text = text, ToolCalls = toolCalls ?? [] };

    /// <summary>Tool natijalari — bitta user xabarida, HAMMASI birga.</summary>
    /// <remarks>
    /// Natijalarni bir nechta xabarga bo'lish modelni parallel tool chaqirishdan jimgina
    /// voz kechishga «o'rgatadi» — shuning uchun ro'yxat bitta xabarga yig'iladi.
    /// </remarks>
    public static LlmMessage FromToolResults(IReadOnlyList<LlmToolResult> results) =>
        new() { Role = LlmRole.User, ToolResults = results };
}

/// <summary>Modelga beriladigan tool ta'rifi.</summary>
/// <param name="Name">Tool kodi.</param>
/// <param name="Description">Model SHUNGA qarab tanlaydi — tavsif tool'ning yagona qo'llanmasi.</param>
/// <param name="Schema">Argumentlarning JSON sxemasi (DTO'dan avtomatik chiqariladi).</param>
public sealed record LlmToolDefinition(string Name, string Description, JsonElement Schema);

/// <summary>Bitta model chaqirig'i.</summary>
/// <remarks>
/// ⚠️ System prompt IKKIGA bo'lingan va bu ataylab. Provayder keshi prefiks bo'yicha ishlaydi
/// (<c>tools</c> → <c>system</c> → <c>messages</c>), ya'ni prefiksning ISTALGAN joyidagi bir
/// bayt o'zgarsa undan keyingi hamma narsa keshdan tushadi. Sana, tenant nomi yoki joriy sahifa
/// barqaror matnga qo'shilsa kesh hech qachon urmaydi — shuning uchun ular
/// <see cref="SystemVolatile"/> da, kesh chegarasidan KEYIN turadi.
/// </remarks>
public sealed record LlmRequest
{
    /// <summary>Barqaror system prompt — qoidalar, uslub. Kesh chegarasi shu blokda.</summary>
    public required string SystemStable { get; init; }

    /// <summary>O'zgaruvchan kontekst: sana, tenant, joriy sahifa. Keshlanmaydi.</summary>
    public string? SystemVolatile { get; init; }

    public required IReadOnlyList<LlmMessage> Messages { get; init; }

    /// <summary>
    /// Foydalanuvchi ruxsatlari bo'yicha FILTRLANGAN tool'lar.
    /// </summary>
    /// <remarks>
    /// Tartib determinstik bo'lishi SHART (registr <c>Code</c> bo'yicha saralaydi): tool ro'yxati
    /// kesh prefiksining eng boshida turadi va tartibi o'zgarsa kesh hech qachon urmaydi.
    /// </remarks>
    public IReadOnlyList<LlmToolDefinition> Tools { get; init; } = [];
}

/// <summary>Model nega to'xtadi.</summary>
public enum LlmStopReason
{
    /// <summary>Javob tugadi.</summary>
    EndTurn = 1,

    /// <summary>Tool chaqirmoqchi — sikl davom etadi.</summary>
    ToolUse = 2,

    /// <summary>Chiqish chegarasiga urildi — javob YARIM.</summary>
    MaxTokens = 3,

    /// <summary>Xavfsizlik siyosati so'rovni rad etdi.</summary>
    Refusal = 4,

    Other = 99,
}

/// <summary>Bitta chaqiriqning token hisobi.</summary>
/// <remarks>
/// To'rt maydon ham alohida: kesh yozish va kesh o'qish oddiy kirishdan BOSHQA tarifda
/// (yozish ~1.25×, o'qish ~0.1×) — ularni bitta «input» ga qo'shish hisobni xato qiladi.
/// </remarks>
/// <param name="InputTokens">Keshdan tashqari kirish.</param>
/// <param name="OutputTokens">Chiqish.</param>
/// <param name="CacheWriteTokens">Keshga yozilgan.</param>
/// <param name="CacheReadTokens">Keshdan o'qilgan — nol bo'lsa kesh ishlamayapti.</param>
public sealed record LlmUsage(long InputTokens, long OutputTokens, long CacheWriteTokens, long CacheReadTokens)
{
    public static readonly LlmUsage Empty = new(0, 0, 0, 0);

    public long Total => InputTokens + OutputTokens + CacheWriteTokens + CacheReadTokens;
}

/// <summary>Model javobi.</summary>
/// <param name="Text">Matn qismi (bo'lmasligi mumkin — faqat tool chaqirsa).</param>
/// <param name="ToolCalls">Chaqirilishi kerak tool'lar.</param>
/// <param name="StopReason">To'xtash sababi.</param>
/// <param name="Usage">Token hisobi — <c>ai_usage</c> ga shu yoziladi.</param>
/// <param name="Model">Haqiqatda ishlagan model (konfigdagidan farq qilishi mumkin).</param>
public sealed record LlmResponse(
    string? Text,
    IReadOnlyList<LlmToolCall> ToolCalls,
    LlmStopReason StopReason,
    LlmUsage Usage,
    string Model);
