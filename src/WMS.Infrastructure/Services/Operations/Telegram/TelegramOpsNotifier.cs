using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Common;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>
/// Ops kanali — <c>telegram_outbox</c> ga tenant va ulanishsiz qator (TG17). Yuboruvchi fon xizmati uni
/// oddiy xabar kabi yuboradi; 403 kelsa (ega botni bloklagan) uzadigan ulanish yo'q — <c>skipped</c>.
/// </summary>
public sealed class TelegramOpsNotifier : IOpsNotifier
{
    private readonly WmsDbContext _db;
    private readonly ITelegramService _telegram;
    private readonly TelegramOptions _options;

    public TelegramOpsNotifier(WmsDbContext db, ITelegramService telegram, IOptions<TelegramOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _db = db;
        _telegram = telegram;
        _options = options.Value;
    }

    public bool IsEnabled => _telegram.IsEnabled && _options.OpsChatId is > 0;

    public async Task SendAsync(string text, string? dedupKey, CancellationToken cancellationToken)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(text)) return;

        if (dedupKey is not null && await _db.TelegramOutboxes.AnyAsync(o => o.DedupKey == dedupKey, cancellationToken))
            return;

        TelegramOutbox outbox = new()
        {
            ChatId = _options.OpsChatId!.Value,
            Text = text.Length > TelegramOutbox.MaxTextLength ? text[..(TelegramOutbox.MaxTextLength - 1)] + "…" : text,
            DedupKey = dedupKey,
        };
        _db.TelegramOutboxes.Add(outbox);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresErrors.IsUniqueViolation(ex))
        {
            // Yuqoridagi tekshiruvdan keyingi poyga — xabar allaqachon navbatda (xato emas).
            // Yozuv kuzatuvdan chiqariladi, aks holda keyingi `SaveChanges` qayta urinardi.
            _db.Entry(outbox).State = EntityState.Detached;
        }
    }
}
