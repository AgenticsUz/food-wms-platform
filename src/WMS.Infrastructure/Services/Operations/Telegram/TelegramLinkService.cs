using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Platform.Infrastructure.Tenancy;
using WMS.Application.Common;
using WMS.Application.DTOs.Notifications;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Operations.Telegram;

/// <summary>Profilning Telegram ulanishi — so'rov ichida, joriy tenant kontekstida.</summary>
public sealed class TelegramLinkService : ITelegramLinkService
{
    /// <summary>24 tasodifiy bayt → 32 belgi base64url; Telegram payload chegarasi 64 belgi <c>[A-Za-z0-9_-]</c>.</summary>
    private const int TokenBytes = 24;

    private readonly WmsDbContext _db;
    private readonly ICurrentTenant _tenant;
    private readonly ITelegramService _telegram;
    private readonly TelegramOptions _options;

    public TelegramLinkService(WmsDbContext db, ICurrentTenant tenant, ITelegramService telegram, IOptions<TelegramOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _db = db;
        _tenant = tenant;
        _telegram = telegram;
        _options = options.Value;
    }

    public async Task<TelegramStatusDto> GetStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        var link = await _db.TelegramLinks.AsNoTracking()
            .Where(l => l.UserProfileId == userId && l.IsActive)
            .Select(l => new { l.LinkedAt, l.Username, l.MutedTypes, l.Digest })
            .FirstOrDefaultAsync(cancellationToken);

        return new TelegramStatusDto
        {
            Enabled = _telegram.IsEnabled,
            BotUsername = _telegram.IsEnabled ? _telegram.BotUsername : null,
            Linked = link is not null,
            LinkedAt = link?.LinkedAt,
            Username = link?.Username,
            MutedTypes = link?.MutedTypes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [],
            Digest = link?.Digest ?? false,
        };
    }

    public async Task<TelegramLinkTokenDto> CreateLinkTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!_telegram.IsEnabled)
            throw new AppException("Telegram bot is not configured");

        // Username startupdagi getMe'dan; u o'tmagan bo'lsa (Telegram vaqtincha yo'q edi) shu yerda qayta uriniladi.
        string? bot = _telegram.BotUsername ?? (await _telegram.GetMeAsync(cancellationToken)).Username;
        if (string.IsNullOrWhiteSpace(bot))
            throw new AppException("Telegram bot is not configured");

        Guid tenantId = _tenant.TenantId ?? throw new AppException("Tenant is required");

        bool profileActive = await _db.UserProfiles.AnyAsync(u => u.Id == userId && u.IsActive, cancellationToken);
        if (!profileActive)
            throw new NotFoundException("User not found");

        DateTime now = DateTime.UtcNow;

        // Bitta faol token: eskilari o'chadi (qattiq — platforma jadvali, tarix kerak emas).
        await _db.TelegramLinkTokens
            .Where(t => t.SubjectType == TelegramLinkSubject.UserProfile && t.SubjectId == userId && t.UsedAt == null)
            .ExecuteDeleteAsync(cancellationToken);

        TelegramLinkToken token = new()
        {
            Token = GenerateToken(),
            TenantId = tenantId,
            SubjectType = TelegramLinkSubject.UserProfile,
            SubjectId = userId,
            ExpiresAt = now.AddMinutes(Math.Max(1, _options.LinkTokenMinutes)),
        };
        _db.TelegramLinkTokens.Add(token);
        await _db.SaveChangesAsync(cancellationToken);

        return new TelegramLinkTokenDto
        {
            Url = $"https://t.me/{bot}?start={token.Token}",
            ExpiresAt = token.ExpiresAt,
        };
    }

    public async Task UnlinkAsync(Guid userId, CancellationToken cancellationToken)
    {
        TelegramLink? link = await _db.TelegramLinks
            .FirstOrDefaultAsync(l => l.UserProfileId == userId && l.IsActive, cancellationToken);
        if (link is null) return;

        link.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetMutedTypesAsync(Guid userId, IReadOnlyCollection<string> types, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(types);

        List<string> normalized = [];
        foreach (string raw in types)
        {
            if (!Enum.TryParse(raw?.Trim(), ignoreCase: true, out NotificationType parsed) || !Enum.IsDefined(parsed))
                throw new AppException("Unknown notification type '{0}'", raw ?? "");
            string name = parsed.ToString();
            if (!normalized.Contains(name, StringComparer.Ordinal)) normalized.Add(name);
        }

        TelegramLink link = await _db.TelegramLinks.FirstOrDefaultAsync(l => l.UserProfileId == userId && l.IsActive, cancellationToken)
            ?? throw new NotFoundException("Telegram is not connected");

        link.MutedTypes = normalized.Count == 0 ? null : string.Join(',', normalized);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetDigestAsync(Guid userId, bool enabled, CancellationToken cancellationToken)
    {
        TelegramLink link = await _db.TelegramLinks.FirstOrDefaultAsync(l => l.UserProfileId == userId && l.IsActive, cancellationToken)
            ?? throw new NotFoundException("Telegram is not connected");
        link.Digest = enabled;
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ── Haydovchi / kontragent (TG12/TG13) ──

    public async Task<TelegramSubjectLinkDto> GetSubjectLinkAsync(TelegramLinkSubject subject, Guid subjectId, CancellationToken cancellationToken)
    {
        var link = await SubjectLinks(subject, subjectId).Where(l => l.IsActive)
            .Select(l => new { l.Username, l.LinkedAt }).FirstOrDefaultAsync(cancellationToken);
        return new TelegramSubjectLinkDto { Enabled = _telegram.IsEnabled, Linked = link is not null, Username = link?.Username, LinkedAt = link?.LinkedAt };
    }

    public async Task<TelegramLinkTokenDto> CreateSubjectLinkTokenAsync(TelegramLinkSubject subject, Guid subjectId, CancellationToken cancellationToken)
    {
        if (!_telegram.IsEnabled) throw new AppException("Telegram bot is not configured");
        string? bot = _telegram.BotUsername ?? (await _telegram.GetMeAsync(cancellationToken)).Username;
        if (string.IsNullOrWhiteSpace(bot)) throw new AppException("Telegram bot is not configured");
        Guid tenantId = _tenant.TenantId ?? throw new AppException("Tenant is required");

        bool exists = subject switch
        {
            TelegramLinkSubject.Driver => await _db.Drivers.AnyAsync(d => d.Id == subjectId && d.IsActive, cancellationToken),
            TelegramLinkSubject.Counterparty => await _db.Counterparties.AnyAsync(c => c.Id == subjectId, cancellationToken),
            _ => false,
        };
        if (!exists) throw new NotFoundException(subject == TelegramLinkSubject.Driver ? "Driver not found" : "Counterparty not found");

        DateTime now = DateTime.UtcNow;
        await _db.TelegramLinkTokens
            .Where(t => t.SubjectType == subject && t.SubjectId == subjectId && t.UsedAt == null)
            .ExecuteDeleteAsync(cancellationToken);

        // Kartadan beriladigan havola odamga telefon/qog'oz orqali yetadi — uzoqroq (1 kun) yashaydi.
        TelegramLinkToken token = new()
        {
            Token = GenerateToken(), TenantId = tenantId, SubjectType = subject, SubjectId = subjectId,
            ExpiresAt = now.AddHours(24),
        };
        _db.TelegramLinkTokens.Add(token);
        await _db.SaveChangesAsync(cancellationToken);
        return new TelegramLinkTokenDto { Url = $"https://t.me/{bot}?start={token.Token}", ExpiresAt = token.ExpiresAt };
    }

    public async Task UnlinkSubjectAsync(TelegramLinkSubject subject, Guid subjectId, CancellationToken cancellationToken)
    {
        TelegramLink? link = await SubjectLinks(subject, subjectId).FirstOrDefaultAsync(l => l.IsActive, cancellationToken);
        if (link is null) return;
        link.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<TelegramLink> SubjectLinks(TelegramLinkSubject subject, Guid subjectId) => subject switch
    {
        TelegramLinkSubject.Driver => _db.TelegramLinks.Where(l => l.DriverId == subjectId),
        TelegramLinkSubject.Counterparty => _db.TelegramLinks.Where(l => l.CounterpartyId == subjectId),
        _ => _db.TelegramLinks.Where(l => l.UserProfileId == subjectId),
    };

    // ── Tenant sozlamasi (TG13) ──

    public async Task<TelegramClientSettingsDto> GetClientSettingsAsync(CancellationToken cancellationToken)
    {
        Guid tenantId = _tenant.TenantId ?? throw new AppException("Tenant is required");
        var t = await _db.Tenants.AsNoTracking().Where(x => x.Id == tenantId)
            .Select(x => new { x.ClientTelegramEnabled, x.DebtReminderDays }).FirstAsync(cancellationToken);
        return new TelegramClientSettingsDto { Enabled = t.ClientTelegramEnabled, DebtReminderDays = t.DebtReminderDays };
    }

    public async Task SetClientSettingsAsync(TelegramClientSettingsDto settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.DebtReminderDays is < 0 or > 90) throw new AppException("Debt reminder interval must be between 0 and 90 days");
        Guid tenantId = _tenant.TenantId ?? throw new AppException("Tenant is required");
        Tenant tenant = await _db.Tenants.FirstAsync(x => x.Id == tenantId, cancellationToken);
        tenant.ClientTelegramEnabled = settings.Enabled;
        tenant.DebtReminderDays = settings.Enabled ? settings.DebtReminderDays : 0;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[TokenBytes];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
