using WMS.Application.DTOs.Notifications;
using WMS.Application.Telegram;

namespace WMS.Application.Interfaces;

/// <summary>
/// Profilning Telegram ulanishi — so'rov ichida, joriy tenant kontekstida (<c>/api/me/telegram</c>).
/// </summary>
/// <remarks>
/// Ilgari <c>INotificationService</c> da <c>Get/SetTelegramChatAsync</c> edi (foydalanuvchi chat
/// ID ni qo'lda kiritardi). Bot foydalanuvchiga u <c>/start</c> bosmaguncha yoza olmaydi (403),
/// shuning uchun endi deep-link: token → <c>t.me/&lt;bot&gt;?start=&lt;token&gt;</c> → bot o'zi ulaydi.
/// </remarks>
public interface ITelegramLinkService
{
    Task<TelegramStatusDto> GetStatusAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Bir martalik token; foydalanuvchining eskirmagan tokenlari bekor qilinadi (bitta faol token).</summary>
    Task<TelegramLinkTokenDto> CreateLinkTokenAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Ulanishni uzadi (<c>is_active = false</c>); yozuv o'chirilmaydi.</summary>
    Task UnlinkAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>O'chirilgan turlar (TG3) — <c>NotificationType</c> nomlari; noma'lum nom → 400. Ro'yxat to'plamni ALMASHTIRADI.</summary>
    Task SetMutedTypesAsync(Guid userId, IReadOnlyCollection<string> types, CancellationToken cancellationToken);

    /// <summary>Kunlik xulosa (TG11) yoqish/o'chirish.</summary>
    Task SetDigestAsync(Guid userId, bool enabled, CancellationToken cancellationToken);
}

/// <summary>
/// Botga kelgan yangilanishni qayta ishlaydi (polling yoki keyin webhook — manbadan mustaqil).
/// </summary>
/// <remarks>
/// Tenant konteksti YO'Q holda chaqiriladi: <c>/start &lt;token&gt;</c> avval platforma jadvalidan
/// tokenni topadi, keyin o'sha tenant uchun ALOHIDA scope ochadi (<c>TenantScopes</c> naqshi).
/// Javoblar darhol, navbatsiz — bu foydalanuvchining HOZIRGI bosishiga javob.
/// </remarks>
public interface ITelegramUpdateHandler
{
    Task HandleAsync(TelegramUpdate update, CancellationToken cancellationToken);
}
