using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Application.Telegram;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;
using WMS.Infrastructure.Tenancy;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>
/// Botga kelgan yangilanish: <c>/start &lt;token&gt;</c> — ulash; bloklash — ulanishni uzish; qolgani — yo'riqnoma.
/// </summary>
/// <remarks>
/// <para>
/// Tenant konteksti YO'Q holda ishlaydi. Token platforma jadvalida (RLS yo'q) topiladi, keyin o'sha
/// tenant uchun ALOHIDA scope ochiladi: <c>app.tenant_id</c> ulanish ochilganda o'qiladi
/// (<c>TenantConnectionInterceptor</c>), bitta scope'da tenantni «o'rtada» almashtirish ishonchsiz.
/// </para>
/// <para>
/// Faqat SHAXSIY chat (v1): guruh yangilanishlari e'tiborsiz (TG16 da oshkora <c>/ulash</c> bilan).
/// Javob darhol, navbatsiz — foydalanuvchining hozirgi bosishiga javob; yetmasa ham ulanish saqlangan.
/// </para>
/// </remarks>
public sealed class TelegramUpdateHandler : ITelegramUpdateHandler
{
    private readonly IServiceProvider _services;
    private readonly ITelegramService _telegram;
    private readonly TelegramCallbackExecutor _callbacks;
    private readonly TelegramQueryCommands _queries;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramUpdateHandler> _logger;

    public TelegramUpdateHandler(IServiceProvider services, ITelegramService telegram, TelegramCallbackExecutor callbacks,
        TelegramQueryCommands queries, IOptions<TelegramOptions> options, ILogger<TelegramUpdateHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _services = services;
        _telegram = telegram;
        _callbacks = callbacks;
        _queries = queries;
        _options = options.Value;
        _logger = logger;
    }

    public async Task HandleAsync(TelegramUpdate update, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);

        if (update.Kind == TelegramUpdateKind.Blocked)
        {
            if (update.ChatId != 0) await DeactivateAsync(update.ChatId, cancellationToken);
            return;
        }

        if (!update.IsPrivate || update.ChatId == 0) return;

        if (update.Kind == TelegramUpdateKind.CallbackQuery)
        {
            if (update.CallbackData?.StartsWith(TelegramQueryCommands.SelectPrefix, StringComparison.Ordinal) == true)
                await _queries.HandleSelectionAsync(update, cancellationToken);
            else
                await _callbacks.ExecuteAsync(update, cancellationToken);
            return;
        }

        if (update.Kind == TelegramUpdateKind.Command && update.Command is { } cmd && TelegramQueryCommands.Commands.Contains(cmd))
        {
            await _queries.HandleCommandAsync(update, cancellationToken);
            return;
        }

        string lang = TelegramLanguage.Resolve(update.LanguageCode, _options.DefaultLanguage);

        switch (update.Kind)
        {
            case TelegramUpdateKind.Start when update.Payload is not null:
                await LinkAsync(update, lang, cancellationToken);
                break;

            case TelegramUpdateKind.Command when update.Command == "status":
                await ReplyAsync(update.ChatId, TelegramBotReplies.Status(await ConnectionsAsync(update.ChatId, cancellationToken), lang), cancellationToken);
                break;

            case TelegramUpdateKind.Command when update.Command == "stop":
                await DeactivateAsync(update.ChatId, cancellationToken);
                await ReplyAsync(update.ChatId, TelegramBotReplies.Stopped(lang), cancellationToken);
                break;

            case TelegramUpdateKind.Start:
            case TelegramUpdateKind.Command:
            case TelegramUpdateKind.Text:
                await ReplyAsync(update.ChatId, TelegramBotReplies.Help(lang), cancellationToken);
                break;

            default:
                break;
        }
    }

    private async Task LinkAsync(TelegramUpdate update, string lang, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;

        // 1-scope, tenantsiz: token — platforma jadvali.
        Guid tokenId;
        Guid tenantId;
        string tenantCode;
        string tenantName;
        TelegramLinkSubject subjectType;
        Guid subjectId;

        await using (AsyncServiceScope lookup = _services.CreateAsyncScope())
        {
            WmsDbContext db = lookup.ServiceProvider.GetRequiredService<WmsDbContext>();
            var found = await (
                from t in db.TelegramLinkTokens.AsNoTracking()
                join tenant in db.Tenants.AsNoTracking() on t.TenantId equals tenant.Id
                where t.Token == update.Payload && t.UsedAt == null && t.ExpiresAt > now && tenant.IsActive
                select new { t.Id, t.TenantId, tenant.Code, tenant.Name, t.SubjectType, t.SubjectId })
                .FirstOrDefaultAsync(cancellationToken);

            if (found is null || found.SubjectType != TelegramLinkSubject.UserProfile)
            {
                // Haydovchi/kontragent tokenlari — TG12/TG13; hozircha ular ham «eskirgan» deb javob oladi.
                await ReplyAsync(update.ChatId, TelegramBotReplies.LinkExpired(lang), cancellationToken);
                return;
            }

            (tokenId, tenantId, tenantCode, tenantName, subjectType, subjectId) =
                (found.Id, found.TenantId, found.Code, found.Name, found.SubjectType, found.SubjectId);
        }

        // 2-scope, tenant bilan: profil va ulanish RLS ostida.
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Set(tenantId, tenantCode);
            WmsDbContext db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();

            bool profileActive = await db.UserProfiles.AnyAsync(p => p.Id == subjectId && p.IsActive, cancellationToken);
            if (!profileActive)
            {
                await ReplyAsync(update.ChatId, TelegramBotReplies.LinkExpired(lang), cancellationToken);
                return;
            }

            TelegramLinkToken token = await db.TelegramLinkTokens.FirstAsync(t => t.Id == tokenId, cancellationToken);
            token.UsedAt = now;

            // Bir profil — bitta yozuv: boshqa akkauntdan qayta ulansa chat ALMASHADI (Wash `Reconnect`).
            TelegramLink? link = await db.TelegramLinks.FirstOrDefaultAsync(l => l.UserProfileId == subjectId, cancellationToken);
            if (link is null)
            {
                link = new TelegramLink { UserProfileId = subjectId };
                db.TelegramLinks.Add(link);
            }

            link.ChatId = update.ChatId;
            link.Username = update.Username;
            link.FirstName = update.FirstName;
            link.Lang = lang;
            link.LinkedVia = TelegramLinkSource.DeepLink;
            link.IsActive = true;
            link.LinkedAt = now;

            await db.SaveChangesAsync(cancellationToken);
            _ = subjectType; // faqat UserProfile (yuqorida tekshirildi); TG12/TG13 shu yerda tarmoqlanadi
        }

        _logger.LogInformation("Telegram: tenant {TenantCode} profili ulandi", tenantCode);
        await ReplyAsync(update.ChatId, TelegramBotReplies.Linked(tenantName, lang), cancellationToken);
    }

    /// <summary><c>/status</c>: chat ulangan tenantlar — har tenantda alohida scope (kam buyruq; N ta scope maqbul).</summary>
    private async Task<IReadOnlyList<TelegramBotReplies.ConnectionLine>> ConnectionsAsync(long chatId, CancellationToken cancellationToken)
    {
        List<TelegramBotReplies.ConnectionLine> lines = [];
        await TenantScopes.ForEachTenantAsync(_services, async (scope, tenantId, ct) =>
        {
            WmsDbContext db = scope.GetRequiredService<WmsDbContext>();
            var found = await db.TelegramLinks.AsNoTracking()
                .Where(l => l.ChatId == chatId && l.IsActive && l.UserProfile != null)
                .Select(l => new { FullName = l.UserProfile!.FullName, l.MutedTypes })
                .FirstOrDefaultAsync(ct);
            if (found is null) return;

            string tenantName = await db.Tenants.AsNoTracking().Where(t => t.Id == tenantId).Select(t => t.Name).FirstAsync(ct);
            int muted = found.MutedTypes?.Split(',', StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
            lines.Add(new TelegramBotReplies.ConnectionLine(tenantName, found.FullName, muted));
        }, _logger, cancellationToken);

        return lines;
    }

    /// <summary>Bloklandi yoki <c>/stop</c> — chat ulangan BARCHA tenantlarda uziladi (kam hodisa; N ta scope maqbul).</summary>
    private async Task DeactivateAsync(long chatId, CancellationToken cancellationToken)
    {
        int deactivated = 0;
        await TenantScopes.ForEachTenantAsync(_services, async (scope, _, ct) =>
        {
            WmsDbContext db = scope.GetRequiredService<WmsDbContext>();
            List<TelegramLink> links = await db.TelegramLinks
                .Where(l => l.ChatId == chatId && l.IsActive)
                .ToListAsync(ct);
            if (links.Count == 0) return;

            foreach (TelegramLink link in links) link.IsActive = false;
            await db.SaveChangesAsync(ct);
            deactivated += links.Count;
        }, _logger, cancellationToken);

        if (deactivated > 0)
            _logger.LogInformation("Telegram: bot bloklandi, {Count} ulanish uzildi", deactivated);
    }

    private async Task ReplyAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        TelegramSendResult result = await _telegram.SendMessageAsync(chatId, text, null, cancellationToken);
        if (!result.Ok)
            _logger.LogInformation("Telegram: javob yuborilmadi (HTTP {StatusCode}: {Description})", result.StatusCode, result.Description);
    }
}
