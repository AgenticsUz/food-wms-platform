using System.Globalization;
using System.Net;
using System.Text.Json;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>Bildirishnomadan Telegram matni (HTML, TG4), tugmalari (TG9) va navbat qatori — bitta joy.</summary>
public static class TelegramOutboxComposer
{
    /// <summary>
    /// <code>
    /// 🏭 <b>Tenant</b>
    /// ✅ <b>Sarlavha</b>
    /// Matn
    ///
    /// <a href="…">Ochish</a>
    /// </code>
    /// Har bo'lak <c>HtmlEncode</c>: matndagi <c>&lt;</c> yoki <c>&amp;</c> (mahsulot nomi, izoh)
    /// kodlanmasa Telegram butun xabarni 400 bilan rad etadi — SQLite davrida shunday xabarlar
    /// jimgina yo'qolardi. Tenant nomi — bir odam ikki zavodda bo'lsa ajrata olsin.
    /// Tugmali xabarda havola tugma bo'ladi, matnda emas.
    /// </summary>
    public static string Text(string? tenantName, Notification notification, string lang, string? linkUrl)
    {
        ArgumentNullException.ThrowIfNull(notification);

        string title = Translations.Format(notification.Title, lang);
        string message = notification.MessageTemplate is { } template
            ? Translations.Format(template, lang, Args(notification.MessageArgs))
            : notification.Message;

        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(tenantName))
            sb.Append("🏭 <b>").Append(WebUtility.HtmlEncode(tenantName)).Append("</b>\n");

        sb.Append(NotificationRouting.Emoji(notification.Type)).Append(" <b>")
            .Append(WebUtility.HtmlEncode(title)).Append("</b>\n")
            .Append(WebUtility.HtmlEncode(message));

        if (!string.IsNullOrWhiteSpace(linkUrl) && !HasButtons(notification.Type))
            sb.Append("\n\n<a href=\"").Append(WebUtility.HtmlEncode(linkUrl)).Append("\">")
                .Append(WebUtility.HtmlEncode(Translations.Format(NotificationMessages.Open, lang))).Append("</a>");

        string text = sb.ToString();
        return text.Length > TelegramOutbox.MaxTextLength ? text[..(TelegramOutbox.MaxTextLength - 1)] + "…" : text;
    }

    /// <summary>Havola: <c>{WebUrl}/{yo'l}</c>; ikkalasidan biri yo'q — havola yo'q.</summary>
    public static string? Link(string? webUrl, Notification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        string? path = NotificationRouting.LinkPath(notification.EntityType, notification.EntityId);
        return string.IsNullOrWhiteSpace(webUrl) || path is null ? null : $"{webUrl.TrimEnd('/')}/{path}";
    }

    public static bool HasButtons(NotificationType type) =>
        type is NotificationType.TransferPending or NotificationType.ProductionPending;

    /// <summary>
    /// Inline tugmalar (TG9). <c>callback_data</c> ≤ 64 bayt: <c>tc:&lt;id N&gt;</c> (35 bayt). Tenant va
    /// aktor unda YO'Q — ular <c>(chat_id, message_id)</c> → navbat qatoridan olinadi.
    /// </summary>
    public static string? ReplyMarkup(Notification notification, string lang, string? linkUrl)
    {
        ArgumentNullException.ThrowIfNull(notification);
        if (notification.EntityId is not { } entityId || !HasButtons(notification.Type)) return null;

        string id = entityId.ToString("N", CultureInfo.InvariantCulture);
        List<object[]> rows = [];

        switch (notification.Type)
        {
            case NotificationType.TransferPending:
                rows.Add([
                    new { text = Translations.Format(TelegramCallbacks.ConfirmButton, lang), callback_data = TelegramCallbacks.TransferConfirm + id },
                    new { text = Translations.Format(TelegramCallbacks.RejectButton, lang), callback_data = TelegramCallbacks.TransferReject + id },
                ]);
                break;
            case NotificationType.ProductionPending:
                rows.Add([new { text = Translations.Format(TelegramCallbacks.StartButton, lang), callback_data = TelegramCallbacks.ProductionStart + id }]);
                break;
            default:
                return null;
        }

        if (!string.IsNullOrWhiteSpace(linkUrl))
            rows.Add([new { text = Translations.Format(NotificationMessages.Open, lang), url = linkUrl }]);

        return JsonSerializer.Serialize(new { inline_keyboard = rows });
    }

    /// <summary>
    /// Bitta bildirishnoma bir chatga bir marta. Hodisa darajasida (tur + entity) EMAS: kam zaxira
    /// har transferda qonuniy ravishda qayta chiqadi — bunday kalit keyingi ogohlantirishlarni yutardi.
    /// </summary>
    public static string DedupKey(Guid notificationId, long chatId) =>
        $"n:{notificationId.ToString("N", CultureInfo.InvariantCulture)}:{chatId.ToString(CultureInfo.InvariantCulture)}";

    public static TelegramOutbox Row(Notification notification, Guid? tenantId, Guid linkId, long chatId, string text, string? replyMarkup) =>
        new()
        {
            TenantId = tenantId,
            TelegramLinkId = linkId,
            ChatId = chatId,
            Text = text,
            ReplyMarkup = replyMarkup,
            NotificationId = notification.Id,
            DedupKey = DedupKey(notification.Id, chatId),
        };

    /// <summary>Yuborilgan tugmali xabardan tugmalarni olib tashlash qatori (amal boshqa joyda bajarildi).</summary>
    public static TelegramOutbox RemoveButtonsRow(TelegramOutbox sent) =>
        new()
        {
            TenantId = sent.TenantId,
            TelegramLinkId = sent.TelegramLinkId,
            ChatId = sent.ChatId,
            Text = string.Empty,
            Kind = TelegramOutboxKind.RemoveButtons,
            MessageId = sent.MessageId,
            NotificationId = sent.NotificationId,
            DedupKey = $"rb:{sent.Id.ToString("N", CultureInfo.InvariantCulture)}",
        };

    /// <summary>JSON satr massivi → argumentlar; buzuq/bo'sh — bo'sh massiv (matn shablon holida qoladi).</summary>
    public static object?[] Args(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<string?[]>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

/// <summary><c>callback_data</c> prefikslari va tugma yozuvlari (Translations kalitlari) — TG9.</summary>
public static class TelegramCallbacks
{
    public const string TransferConfirm = "tc:";
    public const string TransferReject = "tr:";
    public const string ProductionStart = "ps:";

    public const string ConfirmButton = "✅ Confirm";
    public const string RejectButton = "❌ Reject";
    public const string StartButton = "▶️ Start";

    // Tugma javoblari (answerCallbackQuery / xabar ostidagi natija qatori).
    public const string ConfirmedByKey = "✅ Confirmed by {0}, {1}";
    public const string RejectedByKey = "❌ Rejected by {0}, {1}";
    public const string StartedByKey = "▶️ Started by {0}, {1}";
    public const string AlreadyKey = "Already processed by someone else.";
    public const string NoPermissionKey = "You do not have permission for this action.";
    public const string ExpiredKey = "This button has expired.";

    /// <summary>Data → (prefiks, entity id). Tanilmasa <see langword="null"/>.</summary>
    public static (string Action, Guid Id)? Parse(string? data)
    {
        if (data is null || data.Length < 4) return null;
        string action = data[..3];
        if (action is not (TransferConfirm or TransferReject or ProductionStart)) return null;
        return Guid.TryParseExact(data[3..], "N", out Guid id) ? (action, id) : null;
    }
}
