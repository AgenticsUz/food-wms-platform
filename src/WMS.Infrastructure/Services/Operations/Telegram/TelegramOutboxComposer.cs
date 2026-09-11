using System.Globalization;
using System.Net;
using WMS.Domain.Entities;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>Bildirishnomadan Telegram matni va navbat qatori (TG4 shaklni boyitadi — bitta joy).</summary>
public static class TelegramOutboxComposer
{
    /// <summary>
    /// <c>parse_mode=HTML</c>: matndagi <c>&lt;</c> yoki <c>&amp;</c> (mahsulot nomi, izoh) kodlanmasa
    /// Telegram butun xabarni 400 bilan rad etadi — SQLite davrida shunday xabarlar jimgina yo'qolardi.
    /// </summary>
    public static string Text(string title, string message)
    {
        string text = $"<b>{WebUtility.HtmlEncode(title)}</b>\n{WebUtility.HtmlEncode(message)}";
        return text.Length > TelegramOutbox.MaxTextLength ? text[..(TelegramOutbox.MaxTextLength - 1)] + "…" : text;
    }

    /// <summary>
    /// Bitta bildirishnoma bir chatga bir marta. Hodisa darajasida (tur + entity) EMAS: kam zaxira
    /// har transferda qonuniy ravishda qayta chiqadi — bunday kalit keyingi ogohlantirishlarni yutardi.
    /// </summary>
    public static string DedupKey(Guid notificationId, long chatId) =>
        $"n:{notificationId.ToString("N", CultureInfo.InvariantCulture)}:{chatId.ToString(CultureInfo.InvariantCulture)}";

    public static TelegramOutbox Row(Notification notification, Guid? tenantId, Guid linkId, long chatId, string text) =>
        new()
        {
            TenantId = tenantId,
            TelegramLinkId = linkId,
            ChatId = chatId,
            Text = text,
            NotificationId = notification.Id,
            DedupKey = DedupKey(notification.Id, chatId),
        };
}
