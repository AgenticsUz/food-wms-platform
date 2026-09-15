using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

/// <summary>
/// <c>wms.ai_conversation</c> — bitta suhbat (web paneli yoki Telegram chati).
/// </summary>
/// <remarks>
/// <para>
/// Suhbat — AI yozgan hujjatning AUDIT IZI: <c>transfer.ai_conversation_id</c> shu yerga
/// ishora qiladi, ya'ni «bu hujjatni AI qanday so'rov bilan tayyorlagan?» degan savolga
/// javob faqat shu jadvaldan chiqadi. Shuning uchun havola qilingan suhbat tarix
/// tozalashda O'CHIRILMAYDI (FK <c>RESTRICT</c>, <c>AiOptions.HistoryRetentionDays</c> izohi).
/// </para>
/// <para>
/// Telegram tomonda suhbat oynasi <c>telegram_chat_state</c> naqshi bilan bir xil: bir
/// soat jimlikdan keyin yangi suhbat boshlanadi — aks holda ertalabki savol kechqurungi
/// javobga kontekst bo'lib qo'shilardi.
/// </para>
/// </remarks>
public class AiConversation : TenantEntity
{
    public AiChannel Channel { get; set; } = AiChannel.Web;

    /// <summary>Kim gaplashmoqda. Profil o'chirilsa suhbat qoladi (audit izi).</summary>
    public Guid? UserProfileId { get; set; }
    public UserProfile? UserProfile { get; set; }

    /// <summary>Telegram chati (<see cref="AiChannel.Telegram"/> da) — davom etayotgan suhbatni topish uchun.</summary>
    public long? TelegramChatId { get; set; }

    /// <summary>Ro'yxatda ko'rinadigan sarlavha — birinchi savoldan qisqartma.</summary>
    public string? Title { get; set; }

    /// <summary>Suhbat tili (<c>uz</c>/<c>ru</c>) — model shu tilda javob beradi.</summary>
    public string Language { get; set; } = "uz";

    /// <summary>Oxirgi faollik — suhbat oynasi va tarix tozalash shu maydonga qaraydi.</summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    public ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();
}

/// <summary>
/// <c>wms.ai_message</c> — suhbatdagi bitta yozuv.
/// </summary>
/// <remarks>
/// ⚠️ Tool natijalari MIJOZ MA'LUMOTINI (qarz, narx, kontragent nomi) o'z ichiga oladi va
/// shu jadvalda yotadi — jadval RLS ostida va tarix tozalash siyosati unga tegishli.
/// </remarks>
public class AiMessage : TenantEntity
{
    public Guid ConversationId { get; set; }
    public AiConversation Conversation { get; set; } = null!;

    /// <summary>Suhbat ichidagi tartib raqami (1 dan).</summary>
    /// <remarks>
    /// <c>CreatedAt</c> ga tayanib bo'lmaydi: bitta gateway aylanishida yozilgan model javobi
    /// va tool natijalari bir xil lahzaga tushib, tarix tartibi buzilishi mumkin.
    /// </remarks>
    public int Sequence { get; set; }

    public AiMessageRole Role { get; set; }

    public string? Text { get; set; }

    /// <summary>Model chaqirgan tool'lar (JSON massiv) — <see cref="AiMessageRole.Assistant"/> da.</summary>
    public string? ToolCallsJson { get; set; }

    /// <summary>Tool natijalari (JSON massiv) — <see cref="AiMessageRole.Tool"/> da.</summary>
    public string? ToolResultsJson { get; set; }

    /// <summary>Shu xabarni bergan model (javob yozuvlarida).</summary>
    public string? Model { get; set; }

    // ── Token hisobi (faqat model javobida to'la) ──
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int CacheWriteTokens { get; set; }
    public int CacheReadTokens { get; set; }
}

/// <summary>
/// <c>wms.ai_usage</c> — tenantning KUNLIK sarfi (model bo'yicha).
/// </summary>
/// <remarks>
/// <para>
/// Agregat, xabar emas: kvota va hisob-kitob uchun kerak bo'lgani — kunlik yig'indi, va u
/// suhbat tarixidan UZOQROQ yashaydi (tarix 90 kunda tozalanadi, sarf tarixi qoladi).
/// </para>
/// <para>
/// ⚠️ Qator o'qib-o'zgartirib-yozilMAYDI: parallel so'rovlar bir xil kunga yozadi.
/// Yagona to'g'ri yo'l — <c>INSERT … ON CONFLICT DO UPDATE SET x = jadval.x + EXCLUDED.x</c>
/// (<c>DocumentNumbers</c> dagi hisoblagich naqshi).
/// </para>
/// </remarks>
public class AiUsage : TenantEntity
{
    /// <summary>Kun (UTC).</summary>
    public DateOnly Day { get; set; }

    public string Model { get; set; } = null!;

    public int Requests { get; set; }

    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long CacheWriteTokens { get; set; }
    public long CacheReadTokens { get; set; }

    /// <summary>Hisoblangan xarajat (USD).</summary>
    public decimal UsdCost { get; set; }
}

/// <summary>
/// <c>wms.ai_daily_cost</c> — PLATFORMA bo'yicha kunlik sarf. Tenant ustuni YO'Q, RLS yo'q.
/// </summary>
/// <remarks>
/// ⚠️ Nega alohida jadval: <see cref="AiUsage"/> RLS ostida va undan HAMMA tenant bo'yicha
/// yig'indi olib bo'lmaydi — joriy tenant kontekstida so'rov faqat o'z qatorlarini ko'radi.
/// Platforma darajasidagi kunlik shift (<c>Ai:DailyUsdCap</c>) esa aynan shu yig'indiga
/// qaraydi, shuning uchun u tenantdan tashqarida turadigan o'z jadvaliga yoziladi.
/// Tenant ma'lumoti bu yerga TUSHMAYDI: faqat kun, so'rovlar soni va dollar.
/// </remarks>
public class AiDailyCost : BaseEntity
{
    /// <summary>Kun (UTC) — noyob.</summary>
    public DateOnly Day { get; set; }

    public int Requests { get; set; }

    public decimal UsdCost { get; set; }
}
