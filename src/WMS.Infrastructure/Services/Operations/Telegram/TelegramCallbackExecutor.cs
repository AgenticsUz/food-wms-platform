using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;
using WMS.Application.Telegram;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>
/// Inline tugma bosilishi (TG9): navbat qatori → tenant va aktor → ruxsat → amal → audit → javob.
/// </summary>
/// <remarks>
/// <para>
/// <b>Aktor.</b> Web'da amal egasi <c>ICurrentUser</c> (middleware to'ldiradi); bu yerda tenant scope'ida
/// <c>WmsAccessContext</c> ga <c>IWmsAccessResolver</c> natijasi qo'yiladi — ruxsatlar O'SHA RBAC bazasidan,
/// o'sha kesh bilan. Ruxsat BOSILGAN paytda qayta tekshiriladi (rol o'zgargan bo'lishi mumkin).
/// </para>
/// <para>
/// <b>Audit.</b> Web'da audit <c>AuditLogFilter</c> (MVC) yozadi — bot uni chetlab o'tadi, shuning uchun
/// yozuv shu yerda oshkora: <c>Path = telegram:callback</c> bilan jurnalda ajralib turadi.
/// </para>
/// <para>
/// Ikki menejer bir vaqtda bossa: ikkinchisi «allaqachon» javobini oladi — servis holatni tekshiradi
/// (<c>AppException</c>) yoki <c>xmin</c> 409 beradi. Boshqa chatlardagi tugmalar navbat orqali olib
/// tashlanadi (<c>NotificationService</c> tasdiq bildirishnomasida <c>RemoveButtons</c> qatorlarini yozadi).
/// </para>
/// </remarks>
public sealed class TelegramCallbackExecutor
{
    private readonly IServiceProvider _services;
    private readonly ITelegramService _telegram;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramCallbackExecutor> _logger;

    public TelegramCallbackExecutor(IServiceProvider services, ITelegramService telegram,
        IOptions<TelegramOptions> options, ILogger<TelegramCallbackExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _services = services;
        _telegram = telegram;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(TelegramUpdate update, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (update.CallbackId is null) return;

        string lang = TelegramLanguage.Resolve(update.LanguageCode, _options.DefaultLanguage);
        (string Action, Guid Id)? parsed = TelegramCallbacks.Parse(update.CallbackData);
        if (parsed is null || update.MessageId is null)
        {
            await AnswerAsync(update, Translations.Format(TelegramCallbacks.ExpiredKey, lang), true, ct);
            return;
        }

        // 1. Navbat qatori (platforma jadvali) — tenant va qaysi ulanish.
        Guid tenantId; Guid linkId; string tenantCode;
        await using (AsyncServiceScope lookup = _services.CreateAsyncScope())
        {
            WmsDbContext db = lookup.ServiceProvider.GetRequiredService<WmsDbContext>();
            var row = await (
                from o in db.TelegramOutboxes.AsNoTracking()
                join t in db.Tenants.AsNoTracking() on o.TenantId equals t.Id
                where o.ChatId == update.ChatId && o.MessageId == update.MessageId
                      && o.Kind == TelegramOutboxKind.Message && o.TelegramLinkId != null && t.IsActive
                select new { o.TenantId, o.TelegramLinkId, t.Code })
                .FirstOrDefaultAsync(ct);

            if (row is null)
            {
                await AnswerAsync(update, Translations.Format(TelegramCallbacks.ExpiredKey, lang), true, ct);
                return;
            }

            (tenantId, linkId, tenantCode) = (row.TenantId!.Value, row.TelegramLinkId!.Value, row.Code);
        }

        // 2. Tenant scope'i: aktor, ruxsat, amal, audit.
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Set(tenantId, tenantCode);
        IServiceProvider sp = scope.ServiceProvider;
        WmsDbContext tdb = sp.GetRequiredService<WmsDbContext>();

        var actor = await tdb.TelegramLinks.AsNoTracking()
            .Where(l => l.Id == linkId && l.IsActive && l.UserProfile != null && l.UserProfile.IsActive)
            .Select(l => new { l.UserProfile!.IdentitySub, l.UserProfile.FullName, l.Lang })
            .FirstOrDefaultAsync(ct);
        if (actor is null)
        {
            await AnswerAsync(update, Translations.Format(TelegramCallbacks.ExpiredKey, lang), true, ct);
            return;
        }

        lang = TelegramLanguage.Resolve(actor.Lang, _options.DefaultLanguage);

        WmsAccess? access = await sp.GetRequiredService<IWmsAccessResolver>().ResolveAsync(actor.IdentitySub, tenantId, ct);
        sp.GetRequiredService<WmsAccessContext>().Set(access);

        (string action, Guid entityId) = parsed.Value;
        (string permission, string entityType, string entityAction, string doneKey) = action switch
        {
            TelegramCallbacks.TransferConfirm => (WmsPermissions.TransfersConfirm, "Transfers", "Confirm", TelegramCallbacks.ConfirmedByKey),
            TelegramCallbacks.TransferReject => (WmsPermissions.TransfersReject, "Transfers", "Reject", TelegramCallbacks.RejectedByKey),
            _ => (WmsPermissions.ProductionManage, "ProductionOrders", "Start", TelegramCallbacks.StartedByKey),
        };

        if (access is null || !access.Permissions.Contains(permission))
        {
            await AnswerAsync(update, Translations.Format(TelegramCallbacks.NoPermissionKey, lang), true, ct);
            return;
        }

        try
        {
            switch (action)
            {
                case TelegramCallbacks.TransferConfirm:
                    await sp.GetRequiredService<ITransferService>().ConfirmAsync(entityId);
                    break;
                case TelegramCallbacks.TransferReject:
                    await sp.GetRequiredService<ITransferService>().RejectAsync(entityId);
                    break;
                default:
                    await sp.GetRequiredService<IProductionService>().StartOrderAsync(entityId);
                    break;
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            await AnswerAsync(update, Translations.Format(TelegramCallbacks.AlreadyKey, lang), true, ct);
            return;
        }
        catch (AppException ex)
        {
            // «Only pending transfers can be confirmed» va sh.k. — foydalanuvchining tilida.
            await AnswerAsync(update, Translations.Format(ex.MessageTemplate, lang, ex.MessageArgs), true, ct);
            return;
        }

        await WriteAuditAsync(tdb, actor.IdentitySub, actor.FullName, entityType, entityAction, entityId, update.UpdateId, ct);

        string done = Translations.Format(doneKey, lang, actor.FullName, DateTime.UtcNow.AddHours(5).ToString("HH:mm", CultureInfo.InvariantCulture));
        await AnswerAsync(update, done, false, ct);

        // Bosilgan xabar: tugmalar o'rniga natija qatori. Matn navbat qatoridan (o'zgarmagan).
        string? original = await tdb.TelegramOutboxes.AsNoTracking()
            .Where(o => o.ChatId == update.ChatId && o.MessageId == update.MessageId && o.Kind == TelegramOutboxKind.Message)
            .Select(o => o.Text).FirstOrDefaultAsync(ct);
        if (original is not null)
            await _telegram.EditMessageTextAsync(update.ChatId, update.MessageId.Value, original + "\n\n" + done, ct);

        _logger.LogInformation("Telegram: {EntityType}/{Action} bot orqali bajarildi (tenant {TenantCode})", entityType, entityAction, tenantCode);
    }

    private static async Task WriteAuditAsync(WmsDbContext db, Guid sub, string fullName, string entityType, string entityAction,
        Guid entityId, long updateId, CancellationToken ct)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorSub = sub,
            UserName = fullName,
            Action = "POST",
            EntityType = entityType,
            EntityAction = entityAction,
            EntityId = entityId.ToString("D", CultureInfo.InvariantCulture),
            Path = "telegram:callback",
            StatusCode = 200,
            IsPlatformAction = false,
            CorrelationId = "tg:" + updateId.ToString(CultureInfo.InvariantCulture),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    private async Task AnswerAsync(TelegramUpdate update, string text, bool alert, CancellationToken ct)
    {
        if (update.CallbackId is not null)
            await _telegram.AnswerCallbackQueryAsync(update.CallbackId, text, alert, ct);
    }
}
