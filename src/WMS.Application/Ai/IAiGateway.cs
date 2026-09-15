using WMS.Domain.Enums;

namespace WMS.Application.Ai;

/// <summary>Savol so'ragan odam — kanaldan QAT'I NAZAR bir xil shaklda.</summary>
/// <param name="UserProfileId"><c>user_profile.id</c>; suhbat shunga bog'lanadi.</param>
/// <param name="Permissions">Amaldagi ruxsatlar — tool registri shu to'plamga qarab filtrlanadi.</param>
/// <param name="Language">Javob tili (<c>uz</c>/<c>ru</c>).</param>
/// <remarks>
/// ⚠️ Ruxsatlar OSHKORA uzatiladi, <c>ICurrentUser</c> dan olinmaydi: Telegram yangilanishi
/// HTTP so'rovi emas va u yerda «joriy foydalanuvchi» yo'q. Bitta gateway ikkala kanalga
/// xizmat qilishi kerak — demak huquqlar kanalga bog'liq bo'lmagan joyda turishi shart.
/// </remarks>
public sealed record AiUser(Guid? UserProfileId, IReadOnlySet<string> Permissions, string Language);

/// <summary>Bitta savol.</summary>
/// <param name="Channel">Qaysi yuzadan.</param>
/// <param name="Text">Foydalanuvchi matni.</param>
/// <param name="ConversationId">Davom etayotgan suhbat; <see langword="null"/> — yangisi.</param>
/// <param name="TelegramChatId">Telegram chati (o'sha kanalda suhbatni topish uchun).</param>
/// <param name="PageContext">
/// Web'da joriy sahifa va tanlangan ombor — system promptning o'zgaruvchan qismiga tushadi.
/// </param>
public sealed record AiAskRequest(
    AiChannel Channel,
    string Text,
    Guid? ConversationId = null,
    long? TelegramChatId = null,
    string? PageContext = null);

/// <summary>Bitta tool chaqirig'ining natijasi — yuzaga.</summary>
/// <param name="Code">Tool kodi (<c>stock_query</c>) — web qaysi komponent chizishini shundan biladi.</param>
/// <param name="Text">Modelga ketgan matn.</param>
/// <param name="Data">Strukturali natija; <see langword="null"/> — chizadigan narsa yo'q.</param>
public sealed record AiToolOutput(string Code, string Text, object? Data);

/// <summary>Gateway javobi.</summary>
/// <param name="ConversationId">Suhbat — keyingi savol shu id bilan keladi.</param>
/// <param name="Text">Model javobi.</param>
/// <param name="Tools">Shu javob davomida bajarilgan tool'lar (tartibda).</param>
/// <param name="Usage">Jami token sarfi (sikldagi hamma chaqiriq bo'yicha).</param>
public sealed record AiAnswer(
    Guid ConversationId,
    string Text,
    IReadOnlyList<AiToolOutput> Tools,
    LlmUsage Usage);

/// <summary>
/// AI oqimining yagona kirish nuqtasi: savol → (tool'lar) → javob.
/// </summary>
/// <remarks>
/// Web ham, Telegram ham SHU interfeysni chaqiradi. Ikki nusxa bo'lsa, «tool'siz raqamli
/// javob bermaslik» kabi qoidalar bir kanalda kuchga kirib, ikkinchisida jimgina
/// yo'qolardi — va buni faqat mijoz sezgan bo'lardi.
/// </remarks>
public interface IAiGateway
{
    /// <summary>Savolga javob beradi.</summary>
    /// <param name="user">Savol so'ragan odam va uning huquqlari.</param>
    /// <param name="request">Savol.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Javob va bajarilgan tool'lar.</returns>
    /// <exception cref="Common.AiException">
    /// AI o'chiq, kvota tugagan yoki provayder javob bermadi.
    /// </exception>
    Task<AiAnswer> AskAsync(AiUser user, AiAskRequest request, CancellationToken cancellationToken = default);
}
