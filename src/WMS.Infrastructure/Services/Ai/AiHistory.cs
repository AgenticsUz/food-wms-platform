using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Ai;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Ai;

/// <inheritdoc />
public sealed class AiHistory : IAiHistory
{
    private readonly WmsDbContext _db;

    public AiHistory(WmsDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<IReadOnlyList<AiConversationDto>> ListAsync(
        Guid userProfileId, int limit = 20, CancellationToken cancellationToken = default)
    {
        return await _db.AiConversations.AsNoTracking()
            .Where(c => c.UserProfileId == userProfileId)
            .OrderByDescending(c => c.LastActivityAt)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(c => new AiConversationDto(
                c.Id,
                c.Title,
                c.Channel,
                c.LastActivityAt,
                c.Messages.Count(m => !m.IsDeleted)))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AiConversationDetailDto?> GetAsync(
        Guid userProfileId, Guid conversationId, CancellationToken cancellationToken = default)
    {
        // ⚠️ Egasi shartda: RLS tenantni ajratadi, lekin bir tenant ichida boshqa
        // xodimning suhbatini ochish mumkin bo'lib qolardi (sinf izohi).
        AiConversation? conversation = await _db.AiConversations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserProfileId == userProfileId, cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        List<AiMessage> messages = await _db.AiMessages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.Sequence)
            .ToListAsync(cancellationToken);

        return new AiConversationDetailDto(
            new AiConversationDto(
                conversation.Id, conversation.Title, conversation.Channel,
                conversation.LastActivityAt, messages.Count),
            [.. messages.Select(m => new AiMessageDto(
                m.Sequence, m.Role, m.Text, ToolCodes(m.ToolCallsJson), m.CreatedAt))]);
    }

    /// <summary>
    /// Tool chaqiriqlaridan faqat KODLARNI ajratadi.
    /// </summary>
    /// <remarks>
    /// Argumentlar ham, natijalar ham qaytarilmaydi (sababi <see cref="AiMessageDto"/> da).
    /// Buzuq JSON — bo'sh ro'yxat: tarixni ko'rish nosozlik tufayli yiqilmasin.
    /// </remarks>
    private static IReadOnlyList<string> ToolCodes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return [.. document.RootElement.EnumerateArray()
                .Select(e => e.TryGetProperty("name", out JsonElement name) ? name.GetString() : null)
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name!)];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
