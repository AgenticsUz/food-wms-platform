using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Tenancy;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>Chatning bitta tenantdagi ulanishi — buyruq bajarish uchun kerak bo'lgan hamma narsa.</summary>
public sealed record TelegramConnection(Guid TenantId, string TenantCode, string TenantName, Guid LinkId, long ChatId, Guid IdentitySub, Guid ProfileId, string FullName, string Lang);

/// <summary>
/// Shaxsiy chat qaysi tenant(lar)ga ulangan va ko'p tenantli chatda qaysi biri tanlangan (TG10/TG14/TG15).
/// </summary>
/// <remarks>
/// Ulanishlar RLS ostida — har tenantda alohida scope (<see cref="TenantScopes"/>; buyruq kam, N ta scope maqbul).
/// Tanlov <c>telegram_chat_state</c> da (platforma jadvali) bir soat yashaydi.
/// </remarks>
public sealed class TelegramChatContext
{
    public const string SelectPrefix = "sel:";
    private static readonly TimeSpan StateTtl = TimeSpan.FromHours(1);

    private readonly IServiceProvider _services;
    private readonly ITelegramService _telegram;
    private readonly ILogger<TelegramChatContext> _logger;

    public TelegramChatContext(IServiceProvider services, ITelegramService telegram, ILogger<TelegramChatContext> logger)
    {
        _services = services;
        _telegram = telegram;
        _logger = logger;
    }

    public async Task<List<TelegramConnection>> ConnectionsAsync(long chatId, CancellationToken ct)
    {
        List<TelegramConnection> result = [];
        await TenantScopes.ForEachTenantAsync(_services, async (scope, tenantId, token) =>
        {
            WmsDbContext db = scope.GetRequiredService<WmsDbContext>();
            var found = await db.TelegramLinks.AsNoTracking()
                .Where(l => l.ChatId == chatId && l.IsActive && l.UserProfile != null && l.UserProfile.IsActive)
                .Select(l => new { l.Id, l.Lang, l.UserProfile!.IdentitySub, ProfileId = l.UserProfile.Id, l.UserProfile.FullName })
                .FirstOrDefaultAsync(token);
            if (found is null) return;

            var tenant = await db.Tenants.AsNoTracking().Where(t => t.Id == tenantId).Select(t => new { t.Code, t.Name }).FirstAsync(token);
            result.Add(new TelegramConnection(tenantId, tenant.Code, tenant.Name, found.Id, chatId, found.IdentitySub, found.ProfileId, found.FullName, found.Lang));
        }, _logger, ct);
        return result;
    }

    /// <summary>
    /// Buyruq uchun tenant: bitta ulanish — o'sha; bir nechta — eslab qolingan tanlov; yo'q — so'raladi
    /// (inline tugmalar, <c>sel:&lt;tenant&gt;:&lt;buyruq&gt;</c>) va <see langword="null"/> qaytadi.
    /// </summary>
    public async Task<TelegramConnection?> ResolveAsync(long chatId, List<TelegramConnection> connections, string command, string? payload, string lang, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(connections);
        if (connections.Count == 1) return connections[0];

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            WmsDbContext db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
            DateTime now = DateTime.UtcNow;
            Guid? remembered = await db.TelegramChatStates.AsNoTracking()
                .Where(s => s.ChatId == chatId && s.ExpiresAt > now)
                .Select(s => (Guid?)s.TenantId)
                .FirstOrDefaultAsync(ct);
            if (remembered is not null && connections.FirstOrDefault(c => c.TenantId == remembered) is { } chosen)
                return chosen;
        }

        string suffix = payload is null ? command : $"{command} {payload}";
        // callback_data ≤ 64 bayt: "sel:" (4) + 32 + ":" (1) = 37 → buyruq+payload 27 belgigacha.
        if (suffix.Length > 27) suffix = suffix[..27];

        var rows = connections.Select(c => new[] { new { text = c.TenantName, callback_data = $"{SelectPrefix}{c.TenantId:N}:{suffix}" } }).ToArray();
        await _telegram.SendMessageAsync(chatId, Translations.Format(QueryKeys.WhichTenant, lang),
            JsonSerializer.Serialize(new { inline_keyboard = rows }), ct);
        return null;
    }

    public async Task RememberAsync(long chatId, Guid tenantId, CancellationToken ct)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        WmsDbContext db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();
        TelegramChatState? state = await db.TelegramChatStates.FirstOrDefaultAsync(s => s.ChatId == chatId, ct);
        if (state is null)
        {
            state = new TelegramChatState { ChatId = chatId };
            db.TelegramChatStates.Add(state);
        }

        state.TenantId = tenantId;
        state.ExpiresAt = DateTime.UtcNow + StateTtl;
        await db.SaveChangesAsync(ct);
    }

    /// <summary><c>sel:&lt;tenantN&gt;:&lt;buyruq [payload]&gt;</c> → tanlangan ulanish va buyruq.</summary>
    public async Task<(TelegramConnection? Connection, string Command, string? Payload)> SelectAsync(long chatId, string? callbackData, CancellationToken ct)
    {
        string[] parts = (callbackData ?? string.Empty).Split(':', 3);
        if (parts.Length < 3 || !Guid.TryParseExact(parts[1], "N", out Guid tenantId)) return (null, string.Empty, null);

        List<TelegramConnection> connections = await ConnectionsAsync(chatId, ct);
        TelegramConnection? chosen = connections.FirstOrDefault(c => c.TenantId == tenantId);
        if (chosen is null) return (null, string.Empty, null);

        await RememberAsync(chatId, tenantId, ct);
        int space = parts[2].IndexOf(' ', StringComparison.Ordinal);
        return space < 0 ? (chosen, parts[2], null) : (chosen, parts[2][..space], parts[2][(space + 1)..]);
    }
}
