using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;
using WMS.Application.Telegram;
using WMS.Domain.Entities;
using WMS.Domain.Enums;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>
/// Botdagi erkin matn — AI yordamchisiga (F10·A1).
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ Faqat BUYRUQ BO'LMAGAN matn keladi: <c>/qoldiq</c> kabi buyruqlar o'z ishlovchisida
/// qoladi. Sabab — buyruqlar aniq, tez va bepul; ularni AI'ga berish har bosishni pulga
/// aylantirardi va javobni sekinlashtirardi.
/// </para>
/// <para>
/// Oqim: ulanish bormi → tenant aniqmi → <c>ai.chat</c> yoqiqmi → ruxsatlar yechiladi →
/// gateway. Har bosqichda rad javobi BOSHQA matn: «ulanmagansiz», «tenantni tanlang»,
/// «AI yoqilmagan» — foydalanuvchi nima qilishini bilsin.
/// </para>
/// <para>
/// ⚠️ Ruxsatlar gateway'ga OSHKORA uzatiladi: bot so'rovida <c>ICurrentUser</c> yo'q
/// (HTTP qamrovi emas), shuning uchun huquqlar RBAC bazasidan shu yerda yechiladi.
/// </para>
/// </remarks>
public sealed class TelegramAiCommands
{
    /// <summary>Tenant tanlash tugmasida turadigan «buyruq».</summary>
    /// <remarks>
    /// ⚠️ Savol matni tugmaga SIG'MAYDI (<c>callback_data</c> ≤ 64 bayt), shuning uchun
    /// tanlovdan keyin savol qayta so'raladi. Bu faqat ko'p tenantli chatda va faqat bir
    /// marta bo'ladi: tanlov bir soat eslab qolinadi.
    /// </remarks>
    public const string SelectCommand = "ai";

    /// <summary>Telegram bitta xabarga ruxsat beradigan uzunlik.</summary>
    private const int MaxMessageLength = 4096;

    private readonly IServiceProvider _services;
    private readonly ITelegramService _telegram;
    private readonly TelegramChatContext _chats;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramAiCommands> _logger;

    public TelegramAiCommands(
        IServiceProvider services,
        ITelegramService telegram,
        TelegramChatContext chats,
        IOptions<TelegramOptions> options,
        ILogger<TelegramAiCommands> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _services = services;
        _telegram = telegram;
        _chats = chats;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Erkin matnni AI'ga uzatadi.</summary>
    /// <param name="update">Yangilanish.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>
    /// <see langword="true"/> — matn AI tomonidan ishlandi (yoki uning rad javobi yuborildi);
    /// <see langword="false"/> — chat ulanmagan, chaqiruvchi odatdagi yo'riqnomani ko'rsatsin.
    /// </returns>
    public async Task<bool> HandleTextAsync(TelegramUpdate update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);

        string text = (update.Text ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return false;
        }

        string lang = TelegramLanguage.Resolve(update.LanguageCode, _options.DefaultLanguage);

        List<TelegramConnection> connections = await _chats.ConnectionsAsync(update.ChatId, cancellationToken);
        if (connections.Count == 0)
        {
            // Ulanmagan chat — yo'riqnoma ko'rsatilsin (eski xatti-harakat).
            return false;
        }

        TelegramConnection? connection = await _chats.ResolveAsync(
            update.ChatId, connections, SelectCommand, null, lang, cancellationToken);

        if (connection is null)
        {
            // Tugmalar yuborildi; savol tanlovdan keyin qayta so'raladi (izohi `SelectCommand` da).
            return true;
        }

        await AskAsync(update.ChatId, connection, text, lang, cancellationToken);
        return true;
    }

    /// <summary>Tenant tanlash tugmasi bosildi (<c>sel:&lt;tenant&gt;:ai</c>).</summary>
    /// <param name="update">Yangilanish.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Asinxron amal.</returns>
    /// <remarks>
    /// Tanlov <c>SelectAsync</c> ichida eslab qolinadi (bir soat), shuning uchun keyingi
    /// savollarda tugma qayta chiqmaydi.
    /// </remarks>
    public async Task HandleSelectionAsync(TelegramUpdate update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);

        string lang = TelegramLanguage.Resolve(update.LanguageCode, _options.DefaultLanguage);
        (TelegramConnection? chosen, _, _) = await _chats.SelectAsync(update.ChatId, update.CallbackData, cancellationToken);

        if (update.CallbackId is not null)
        {
            await _telegram.AnswerCallbackQueryAsync(
                update.CallbackId,
                chosen?.TenantName ?? Translations.Format(TelegramCallbacks.ExpiredKey, lang),
                false,
                cancellationToken);
        }

        if (chosen is null)
        {
            return;
        }

        if (update.MessageId is { } messageId)
        {
            await _telegram.EditMessageTextAsync(
                update.ChatId, messageId, "🏭 " + WebUtility.HtmlEncode(chosen.TenantName), cancellationToken);
        }

        await SendAsync(update.ChatId, Translations.Format(TelegramAiKeys.AskAgain, lang), cancellationToken);
    }

    /// <summary>Bu tugma AI tanlovimi (<c>sel:&lt;tenant&gt;:ai</c>).</summary>
    /// <param name="callbackData">Tugma ma'lumoti.</param>
    /// <returns>Shundaymi.</returns>
    public static bool IsSelection(string? callbackData)
    {
        string[] parts = (callbackData ?? string.Empty).Split(':', 3);
        return parts.Length == 3 && string.Equals(parts[2], SelectCommand, StringComparison.Ordinal);
    }

    private async Task AskAsync(
        long chatId, TelegramConnection connection, string text, string lang, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        // ⚠️ Tenant BIRINCHI baza so'rovidan oldin: `app.tenant_id` ulanish ochilgan
        // lahzada o'qiladi (`TenantConnectionInterceptor`).
        services.GetRequiredService<ICurrentTenant>().Set(connection.TenantId, connection.TenantCode);

        WmsAccess? access = await services.GetRequiredService<IWmsAccessResolver>()
            .ResolveAsync(connection.IdentitySub, connection.TenantId, cancellationToken);

        if (access is null)
        {
            await SendAsync(chatId, Translations.Format(TelegramCallbacks.NoPermissionKey, lang), cancellationToken);
            return;
        }

        // «Yozmoqda…» — AI javobi bir necha soniya oladi va jimlik «bot o'lgan» degan
        // taassurot berardi. Natija tekshirilmaydi (ko'rsatkich — bezak, javob emas).
        await _telegram.SendTypingAsync(chatId, cancellationToken);

        AiUser user = new(access.ProfileId, access.Permissions, lang);
        AiAskRequest request = new(AiChannel.Telegram, text, TelegramChatId: chatId);

        try
        {
            AiAnswer answer = await services.GetRequiredService<IAiGateway>()
                .AskAsync(user, request, cancellationToken);

            await SendAsync(chatId, answer.Text, cancellationToken);
        }
        catch (AiException ex)
        {
            // Rad javoblari foydalanuvchi TILIDA va sababi bilan: «yoqilmagan», «kvota
            // tugadi» va «vaqtincha ishlamayapti» — uchta butunlay boshqa xabar.
            await SendAsync(chatId, Translations.Format(ex.MessageTemplate, lang, ex.MessageArgs), cancellationToken);
        }
#pragma warning disable CA1031 // Bot bitta savol tufayli jim qolmasin.
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            _logger.LogError(ex, "Telegram AI javobi yiqildi (chat {ChatId})", chatId);
            await SendAsync(chatId, Translations.Format(TelegramAiKeys.Failed, lang), cancellationToken);
        }
    }

    /// <summary>
    /// Javobni yuboradi; uzun bo'lsa bo'lib yuboradi.
    /// </summary>
    /// <remarks>
    /// ⚠️ Telegram 4096 belgidan uzun xabarni RAD ETADI. Kesib yuborish ham yaramaydi —
    /// javobning oxiri (ko'pincha xulosa) yo'qolardi. Bo'linish qator chegarasida qilinadi:
    /// so'z o'rtasidan kesilgan matn o'qilmaydi.
    /// </remarks>
    private async Task SendAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        foreach (string part in Split(WebUtility.HtmlEncode(text)))
        {
            TelegramSendResult result = await _telegram.SendMessageAsync(chatId, part, null, cancellationToken);
            if (!result.Ok)
            {
                _logger.LogInformation(
                    "Telegram: AI javobi yuborilmadi (HTTP {StatusCode}: {Description})",
                    result.StatusCode, result.Description);
                return;
            }
        }
    }

    private static IEnumerable<string> Split(string text)
    {
        if (text.Length <= MaxMessageLength)
        {
            yield return text;
            yield break;
        }

        int start = 0;
        while (start < text.Length)
        {
            int length = Math.Min(MaxMessageLength, text.Length - start);

            if (start + length < text.Length)
            {
                int line = text.LastIndexOf('\n', start + length - 1, length);
                if (line > start)
                {
                    length = line - start + 1;
                }
            }

            yield return text.Substring(start, length);
            start += length;
        }
    }
}

/// <summary>AI bot javoblarining matn kalitlari (<c>Translations</c>).</summary>
public static class TelegramAiKeys
{
    /// <summary>Ko'p tenantli chatda tanlovdan keyin.</summary>
    public const string AskAgain = "Organization selected. Please repeat your question.";

    /// <summary>Kutilmagan nosozlik.</summary>
    public const string Failed = "Could not answer right now. Please try again.";
}
