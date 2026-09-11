using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Application.Telegram;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>
/// Botga kelgan yangilanishlarni <c>getUpdates</c> long-poll bilan oladi va har birini alohida
/// scope'da <see cref="ITelegramUpdateHandler"/> ga beradi.
/// </summary>
/// <remarks>
/// <para>
/// Polling, webhook emas (TZ qarori): bitta API instansi, bitta umumiy bot; ochiq endpoint, sir
/// va dev'da tunnel kerak emas. Bir tokenni ikki instans polling qilsa Telegram 409 beradi —
/// dev va prod BOSHQA botlar. Webhook keyin shu handler'ga keladi.
/// </para>
/// <para>
/// Xizmat HECH QACHON o'lmaydi: tarmoq xatosi — kutib qayta urinish; bitta buzuq yangilanish —
/// logga, keyingisiga o'tiladi. Token yaroqsiz bo'lsa (<c>getMe</c> 401) bot o'chiq deb qabul
/// qilinadi, API ko'tarilaveradi.
/// </para>
/// </remarks>
public sealed class TelegramPollingBackgroundService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan InitRetryDelay = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelegramService _telegram;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramPollingBackgroundService> _logger;

    public TelegramPollingBackgroundService(IServiceScopeFactory scopeFactory, ITelegramService telegram,
        IOptions<TelegramOptions> options, ILogger<TelegramPollingBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _scopeFactory = scopeFactory;
        _telegram = telegram;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_telegram.IsEnabled) return;

        // Migrator va birinchi so'rovlar bilan talashmasin.
        await Task.Delay(StartupDelay, stoppingToken);

        // getMe o'tmasa (Telegram vaqtincha yo'q yoki token yaroqsiz) — har daqiqa qayta uriniladi;
        // token haqiqatan yaroqsiz bo'lsa log daqiqasiga bir ogohlantiradi, API ishlayveradi.
        while (!await InitializeBotAsync(stoppingToken))
            await Task.Delay(InitRetryDelay, stoppingToken);

        TimeSpan retryDelay = TimeSpan.FromSeconds(Math.Max(1, _options.PollingIntervalSeconds));
        long? offset = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                IReadOnlyList<TelegramUpdate> updates =
                    await _telegram.GetUpdatesAsync(offset, Math.Max(1, _options.LongPollTimeoutSeconds), stoppingToken);

                foreach (TelegramUpdate update in updates)
                {
                    // Offset AVVAL suriladi: buzuq yangilanish qayta-qayta o'qilmasin.
                    offset = update.UpdateId + 1;
                    await HandleOneAsync(update, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // Polling yiqilsa xizmat to'xtamasin — kutib qayta uriniladi.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                // InvalidOperationException — Telegram `description` (xavfsiz); qolganida faqat tur
                // (HttpRequestException xabarida manzil bo'lishi mumkin).
                string reason = ex is InvalidOperationException ? ex.Message : ex.GetType().Name;
                _logger.LogWarning("Telegram polling xatosi: {Reason} — {Delay}s dan keyin qayta", reason, retryDelay.TotalSeconds);
                await Task.Delay(retryDelay, stoppingToken);
            }
        }
    }

    /// <summary><c>getMe</c> (username keshi), webhook o'chirish, buyruqlar menyusi.</summary>
    private async Task<bool> InitializeBotAsync(CancellationToken cancellationToken)
    {
        TelegramBotInfo me = await _telegram.GetMeAsync(cancellationToken);
        if (!me.Ok)
        {
            _logger.LogWarning("Telegram getMe muvaffaqiyatsiz ({Error}) — {Delay}s dan keyin qayta", me.Error, InitRetryDelay.TotalSeconds);
            return false;
        }

        // Polling va webhook bir vaqtda ishlamaydi.
        await _telegram.DeleteWebhookAsync(cancellationToken);

        // Buyruqlar menyusi KODDAN (BotFather'da qo'lda qo'yilmaydi — ikki manba bo'lardi). Tilsiz
        // ro'yxat — sukut (uz); Telegram foydalanuvchining tiliga qarab tanlaydi.
        TelegramBotCommand[] uz = [new("start", "Ulash"), new("help", "Yordam")];
        TelegramBotCommand[] ru = [new("start", "Подключить"), new("help", "Помощь")];
        await _telegram.SetMyCommandsAsync(uz, null, cancellationToken);
        await _telegram.SetMyCommandsAsync(uz, "uz", cancellationToken);
        await _telegram.SetMyCommandsAsync(ru, "ru", cancellationToken);

        _logger.LogInformation("Telegram bot @{Bot} ishga tushdi (polling)", me.Username);
        return true;
    }

    private async Task HandleOneAsync(TelegramUpdate update, CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        try
        {
            await scope.ServiceProvider.GetRequiredService<ITelegramUpdateHandler>().HandleAsync(update, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Telegram yangilanishi {UpdateId} qayta ishlanmadi", update.UpdateId);
        }
    }
}
