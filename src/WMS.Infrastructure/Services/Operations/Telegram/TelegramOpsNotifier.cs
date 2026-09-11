using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
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

        _db.TelegramOutboxes.Add(new TelegramOutbox
        {
            ChatId = _options.OpsChatId!.Value,
            Text = text.Length > TelegramOutbox.MaxTextLength ? text[..(TelegramOutbox.MaxTextLength - 1)] + "…" : text,
            DedupKey = dedupKey,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
