namespace WMS.Application.Interfaces;

/// <summary>
/// Foydalanuvchi bo'lmagan aktorlarga Telegram (TG12 haydovchi, TG13 kontragent). Joriy tenant
/// kontekstida, navbat orqali (so'rov ichida HTTP yo'q). Ulanmagan yoki tenant ruxsat bermagan — no-op.
/// </summary>
public interface ITelegramPartnerNotifier
{
    /// <summary>Haydovchiga bugungi marshrut: to'xtashlar va «yetkazildi/yetkazilmadi» tugmalari.</summary>
    Task SendRouteAsync(Guid deliveryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mijozga xabar — faqat <c>Tenant.ClientTelegramEnabled</c> bo'lsa. <paramref name="template"/> —
    /// <c>NotificationMessages.Client*</c> kaliti; {0} har doim zavod nomi (bu yerda qo'shiladi).
    /// </summary>
    Task NotifyClientAsync(Guid counterpartyId, string template, string?[] args, string? dedupKey = null, CancellationToken cancellationToken = default);
}
