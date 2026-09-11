using WMS.Application.Common;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>Tinch soatlar va kunlik soat hisoblari — Toshkent vaqti (UTC+5, HOLAT «keyinga qolgan» qaror).</summary>
public static class TelegramQuietHours
{
    public static readonly TimeSpan TashkentOffset = TimeSpan.FromHours(5);

    public static DateTime ToTashkent(DateTime utc) => utc + TashkentOffset;

    /// <summary>
    /// Xabar qachon yuborilsin: shoshilinch — darhol; oddiy — tinch soatlarda bo'lsa, tinch soat tugashida.
    /// <see langword="null"/> — darhol.
    /// </summary>
    public static DateTime? HoldUntil(DateTime utcNow, NotificationType type, TelegramOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (NotificationRouting.IsUrgent(type) || options.QuietFromHour == options.QuietToHour) return null;

        DateTime local = ToTashkent(utcNow);
        int hour = local.Hour;
        bool overnight = options.QuietFromHour > options.QuietToHour; // 22 → 7
        bool quiet = overnight
            ? hour >= options.QuietFromHour || hour < options.QuietToHour
            : hour >= options.QuietFromHour && hour < options.QuietToHour;
        if (!quiet) return null;

        DateTime release = local.Date.AddHours(options.QuietToHour);
        if (release <= local) release = release.AddDays(1);
        return DateTime.SpecifyKind(release - TashkentOffset, DateTimeKind.Utc);
    }

    /// <summary>Toshkent vaqtida shu soat boshlanganmi va bugun hali bajarilmaganmi (fon xizmatining daqiqalik tekshiruvi).</summary>
    public static bool IsDue(DateTime utcNow, int hour, DateOnly? lastRunLocalDate)
    {
        DateTime local = ToTashkent(utcNow);
        DateOnly today = DateOnly.FromDateTime(local);
        return local.Hour >= hour && lastRunLocalDate != today;
    }
}
