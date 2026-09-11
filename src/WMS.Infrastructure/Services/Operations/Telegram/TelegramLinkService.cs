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

    private static string GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[TokenBytes];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
