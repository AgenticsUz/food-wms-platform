using WMS.Domain.Enums;

namespace WMS.Application.Ai;

/// <summary>Ro'yxatdagi suhbat.</summary>
/// <param name="Id">Suhbat.</param>
/// <param name="Title">Sarlavha (birinchi savoldan qisqartma).</param>
/// <param name="Channel">Kanal — web va bot suhbatlari bir ro'yxatda ko'rinadi.</param>
/// <param name="LastActivityAt">Oxirgi faollik.</param>
/// <param name="MessageCount">Xabarlar soni.</param>
public sealed record AiConversationDto(
    Guid Id, string? Title, AiChannel Channel, DateTime LastActivityAt, int MessageCount);

/// <summary>Suhbatdagi bitta xabar.</summary>
/// <param name="Sequence">Tartib raqami.</param>
/// <param name="Role">Kim yozgan.</param>
/// <param name="Text">Matn (tool yozuvida bo'sh).</param>
/// <param name="Tools">Shu qadamda bajarilgan tool kodlari.</param>
/// <param name="CreatedAt">Yozilgan lahza.</param>
/// <remarks>
/// ⚠️ Tool natijalarining TO'LIQ JSON'i qaytarilMAYDI — faqat kodlari. Tarixda mijoz
/// ma'lumoti (qarz, narx) yotadi va uni «suhbatni ko'rish» yuzasi orqali qaytarish
/// ruxsat chegarasini aylanib o'tish yo'li bo'lardi: suhbat yozilganda ruxsat BOR edi,
/// hozir bo'lmasligi mumkin.
/// </remarks>
public sealed record AiMessageDto(
    int Sequence, AiMessageRole Role, string? Text, IReadOnlyList<string> Tools, DateTime CreatedAt);

/// <summary>Suhbat va uning xabarlari.</summary>
/// <param name="Conversation">Suhbat.</param>
/// <param name="Messages">Xabarlar (eskisi birinchi).</param>
public sealed record AiConversationDetailDto(AiConversationDto Conversation, IReadOnlyList<AiMessageDto> Messages);

/// <summary>
/// Suhbat tarixi — web panelidagi «oldingi suhbatlar» ro'yxati.
/// </summary>
/// <remarks>
/// ⚠️ Faqat SO'RAGAN ODAMNING suhbatlari: RLS tenantni ajratadi, lekin bir tenant ichida
/// bir xodimning savollari ikkinchisiga ko'rinmasligi kerak — savolning o'zida ham
/// ma'lumot bo'ladi («Korzinka qarzi qancha?»).
/// </remarks>
public interface IAiHistory
{
    /// <summary>Foydalanuvchining suhbatlari (yangi faollik birinchi).</summary>
    /// <param name="userProfileId">Kimning suhbatlari.</param>
    /// <param name="limit">Ko'pi bilan nechta.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Suhbatlar.</returns>
    Task<IReadOnlyList<AiConversationDto>> ListAsync(
        Guid userProfileId, int limit = 20, CancellationToken cancellationToken = default);

    /// <summary>Bitta suhbat va uning xabarlari.</summary>
    /// <param name="userProfileId">So'rayotgan odam.</param>
    /// <param name="conversationId">Suhbat.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Suhbat; topilmasa yoki boshqanikiga tegishli bo'lsa <see langword="null"/>.</returns>
    Task<AiConversationDetailDto?> GetAsync(
        Guid userProfileId, Guid conversationId, CancellationToken cancellationToken = default);
}
