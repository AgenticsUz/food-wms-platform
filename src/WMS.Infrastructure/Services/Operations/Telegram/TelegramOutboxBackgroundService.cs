using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>
/// <c>telegram_outbox</c> navbatini yuboradi: tezlik chegarasi, qayta urinish, bloklangan chatni uzish, tozalash.
/// </summary>
/// <remarks>
/// <para>
/// Navbat platforma jadvali — bitta so'rovda hamma tenantniki o'qiladi; lekin ulanish (<c>telegram_link</c>)
/// RLS ostida, shuning uchun partiya tenant bo'yicha guruhlanadi va har guruh O'Z scope'ida yuboriladi.
/// Har qator ALOHIDA saqlanadi: yarim yuborilgan partiya restartda takrorlanmasin (Wash izohi).
/// </para>
/// <para>
/// Telegram chegaralari: ~30 xabar/s umumiy, 1 xabar/s bir chatga. Sekin va ishonchli — tez va
/// bloklangan emas: qatorlar orasida qisqa pauza, bir chatga soniyada bittadan ko'p emas.
/// </para>
/// </remarks>
public sealed class TelegramOutboxBackgroundService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan PerChatGap = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan GlobalGap = TimeSpan.FromMilliseconds(40);
    private static readonly TimeSpan RateLimitFallback = TimeSpan.FromSeconds(5);
    private const int MaxErrorLength = 500;
    private const int ChatClockLimit = 10_000;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelegramService _telegram;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramOutboxBackgroundService> _logger;
    private readonly Dictionary<long, DateTime> _lastSendByChat = new();
    private DateTime _lastCleanup = DateTime.MinValue;

    public TelegramOutboxBackgroundService(IServiceScopeFactory scopeFactory, ITelegramService telegram,
        IOptions<TelegramOptions> options, ILogger<TelegramOutboxBackgroundService> logger)
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

        await Task.Delay(StartupDelay, stoppingToken);
        TimeSpan interval = TimeSpan.FromSeconds(Math.Max(1, _options.PollingIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // Navbat yugurishi yiqilsa xizmat to'xtamasin.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogError(ex, "Telegram navbati yugurishi yiqildi");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        DateTime now = DateTime.UtcNow;

        List<PendingRow> batch;
        Dictionary<Guid, string> tenantCodes;
        await using (AsyncServiceScope listing = _scopeFactory.CreateAsyncScope())
        {
            WmsDbContext db = listing.ServiceProvider.GetRequiredService<WmsDbContext>();
            batch = await db.TelegramOutboxes.AsNoTracking()
                .Where(o => o.Status == TelegramOutboxStatus.Pending && (o.NextAttemptAt == null || o.NextAttemptAt <= now))
                .OrderBy(o => o.CreatedAt)
                .Take(Math.Max(1, _options.OutboxBatchSize))
                .Select(o => new PendingRow(o.Id, o.TenantId, o.TelegramLinkId))
                .ToListAsync(ct);

            if (now - _lastCleanup > CleanupInterval)
            {
                await CleanupAsync(db, now, ct);
                _lastCleanup = now;
            }

            if (batch.Count == 0) return;

            List<Guid> tenantIds = batch.Where(b => b.TenantId != null).Select(b => b.TenantId!.Value).Distinct().ToList();
            tenantCodes = await db.Tenants.AsNoTracking()
                .Where(t => tenantIds.Contains(t.Id))
                .ToDictionaryAsync(t => t.Id, t => t.Code, ct);
        }

        int sent = 0, skipped = 0, failed = 0, retried = 0;

        foreach (IGrouping<Guid?, PendingRow> group in batch.GroupBy(b => b.TenantId))
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            if (group.Key is { } tenantId)
                scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Set(tenantId, tenantCodes.GetValueOrDefault(tenantId));

            WmsDbContext db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();

            List<Guid> linkIds = group.Where(g => g.TelegramLinkId != null).Select(g => g.TelegramLinkId!.Value).ToList();
            Dictionary<Guid, TelegramLink> links = linkIds.Count == 0
                ? new()
                : await db.TelegramLinks.Where(l => linkIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);

            foreach (PendingRow pending in group)
            {
                TelegramOutbox? row = await db.TelegramOutboxes.FirstOrDefaultAsync(o => o.Id == pending.Id, ct);
                if (row is null || row.Status != TelegramOutboxStatus.Pending) continue;

                TelegramLink? link = row.TelegramLinkId is { } linkId ? links.GetValueOrDefault(linkId) : null;
                if (row.TelegramLinkId is not null && (link is null || !link.IsActive))
                {
                    // Yozilgandan keyin uzilgan/bloklangan — xato emas.
                    row.Status = TelegramOutboxStatus.Skipped;
                    row.LastError = "link inactive";
                    skipped++;
                    await db.SaveChangesAsync(ct);
                    continue;
                }

                await ThrottleAsync(row.ChatId, ct);
                TelegramSendResult result = row.Kind == TelegramOutboxKind.RemoveButtons
                    ? await RemoveButtonsAsync(row, ct)
                    : await _telegram.SendMessageAsync(row.ChatId, row.Text, row.ReplyMarkup, ct);
                DateTime at = DateTime.UtcNow;

                if (result.Ok)
                {
                    row.Status = TelegramOutboxStatus.Sent;
                    row.SentAt = at;
                    row.LastError = null;
                    row.NextAttemptAt = null;
                    if (row.Kind == TelegramOutboxKind.Message) row.MessageId = result.MessageId;
                    sent++;
                }
                else if (result.IsRateLimited)
                {
                    // Urinish sanalmaydi — bu bizning tezligimiz, xabarning aybi emas.
                    row.NextAttemptAt = at + (result.RetryAfterSeconds is { } s ? TimeSpan.FromSeconds(s) : RateLimitFallback);
                    row.LastError = Trim($"429 {result.Description}");
                    retried++;
                }
                else if (result.IsChatGone)
                {
                    // Foydalanuvchi botni bloklagan yoki chat yo'q — qayta urinish befoyda, ulanish uziladi.
                    row.Status = TelegramOutboxStatus.Skipped;
                    row.LastError = Trim($"{result.StatusCode} {result.Description}");
                    if (link is not null) link.IsActive = false;
                    else if (row.TelegramLinkId is null && row.TenantId is not null)
                    {
                        // Guruh (TG16): bot guruhdan chiqarilgan — guruh uziladi.
                        await db.TelegramGroups.Where(g => g.ChatId == row.ChatId && g.IsActive)
                            .ExecuteUpdateAsync(s => s.SetProperty(g => g.IsActive, false), ct);
                    }
                    skipped++;
                    _logger.LogInformation("Telegram: chat yopiq (HTTP {StatusCode}), ulanish uzildi", result.StatusCode);
                }
                else
                {
                    row.Attempts++;
                    row.LastError = Trim($"{result.StatusCode} {result.Description}");
                    if (row.Attempts >= TelegramOutbox.MaxAttempts)
                    {
                        row.Status = TelegramOutboxStatus.Failed;
                        row.NextAttemptAt = null;
                        failed++;
                    }
                    else
                    {
                        row.NextAttemptAt = at + TelegramOutbox.BackoffFor(row.Attempts);
                        retried++;
                    }
                }

                // Har qator alohida — yuqoridagi izoh.
                await db.SaveChangesAsync(ct);
            }
        }

        _logger.LogInformation("Telegram navbati: {Sent} yuborildi, {Skipped} o'tkazildi, {Failed} xato, {Retried} qayta urinishda",
            sent, skipped, failed, retried);

        // Bir yugurishda ko'p yakuniy xato — egaga (TG17), soatiga bir marta.
        if (failed >= FailedAlertThreshold && DateTime.UtcNow - _lastFailureAlert > TimeSpan.FromHours(1))
        {
            _lastFailureAlert = DateTime.UtcNow;
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IOpsNotifier>().SendAsync(
                $"🔴 Telegram navbati: bitta yugurishda {failed} ta xabar yakuniy xato bilan tugadi", null, ct);
        }
    }

    private const int FailedAlertThreshold = 5;
    private DateTime _lastFailureAlert = DateTime.MinValue;

    /// <summary>
    /// Tugmalarni olib tashlash (TG9). Xabar allaqachon tahrirlangan/o'chirilgan bo'lsa Telegram 400
    /// («message is not modified» / «message to edit not found») beradi — bu xato emas, qator yopiladi.
    /// </summary>
    private async Task<TelegramSendResult> RemoveButtonsAsync(TelegramOutbox row, CancellationToken ct)
    {
        if (row.MessageId is not { } messageId) return TelegramSendResult.Success(null);
        bool ok = await _telegram.RemoveReplyMarkupAsync(row.ChatId, messageId, ct);
        return TelegramSendResult.Success(null) with { Ok = true, Description = ok ? null : "edit skipped" };
    }

    /// <summary>Bir chatga soniyada bittadan ko'p emas; umumiy oqimda qatorlar orasida qisqa pauza.</summary>
    private async Task ThrottleAsync(long chatId, CancellationToken ct)
    {
        if (_lastSendByChat.Count > ChatClockLimit) _lastSendByChat.Clear();

        if (_lastSendByChat.TryGetValue(chatId, out DateTime last))
        {
            TimeSpan wait = PerChatGap - (DateTime.UtcNow - last);
            if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
        }
        else
        {
            await Task.Delay(GlobalGap, ct);
        }

        _lastSendByChat[chatId] = DateTime.UtcNow;
    }

    /// <summary>Yakunlangan qatorlar va eskirgan tokenlar — kuniga bir marta.</summary>
    private async Task CleanupAsync(WmsDbContext db, DateTime now, CancellationToken ct)
    {
        DateTime cutoff = now.AddDays(-Math.Max(1, _options.OutboxRetentionDays));
        int rows = await db.TelegramOutboxes
            .Where(o => o.Status != TelegramOutboxStatus.Pending && o.CreatedAt < cutoff)
            .ExecuteDeleteAsync(ct);

        int tokens = await db.TelegramLinkTokens
            .Where(t => t.ExpiresAt < now.AddDays(-1))
            .ExecuteDeleteAsync(ct);

        if (rows + tokens > 0)
            _logger.LogInformation("Telegram tozalash: {Rows} navbat qatori, {Tokens} token o'chirildi", rows, tokens);
    }

    private static string Trim(string text) => text.Length > MaxErrorLength ? text[..MaxErrorLength] : text;

    private sealed record PendingRow(Guid Id, Guid? TenantId, Guid? TelegramLinkId);
}
