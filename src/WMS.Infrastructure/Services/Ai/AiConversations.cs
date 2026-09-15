using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WMS.Application.Ai;
using WMS.Domain.Entities;
using WMS.Domain.Enums;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Ai;

/// <summary>
/// Suhbat oynasi va tarixi: ochish, o'qish, yozish.
/// </summary>
/// <remarks>
/// <para>
/// <b>Oyna — bir soat jimlik va oxirgi 10 xabar.</b> Telegram tomonda bu
/// <c>telegram_chat_state</c> naqshi bilan bir xil: ertalabki savol kechqurungi javobga
/// kontekst bo'lib qo'shilmasin. Uzunlik chegarasi esa xarajat masalasi — butun tarix har
/// so'rovda qayta yuborilardi.
/// </para>
/// <para>
/// ⚠️ <b>Tarix KESILGANDA juftlik buzilmasligi shart.</b> Provayder <c>tool_use</c> bloki
/// ortidan <c>tool_result</c> kelishini talab qiladi. Oxirgi 10 xabarni ko'r-ko'rona olish
/// birinchi xabar sifatida «tool natijasi»ni qoldirib ketishi mumkin va so'rov 400 bilan
/// rad etilardi — shuning uchun boshidan birinchi FOYDALANUVCHI xabarigacha tashlanadi.
/// </para>
/// </remarks>
internal sealed class AiConversations
{
    /// <summary>Suhbat oynasi — shuncha jimlikdan keyin yangisi boshlanadi.</summary>
    private static readonly TimeSpan Window = TimeSpan.FromHours(1);

    /// <summary>Modelga beriladigan oxirgi xabarlar soni.</summary>
    private const int HistoryLimit = 10;

    private readonly WmsDbContext _db;

    public AiConversations(WmsDbContext db) => _db = db;

    /// <summary>Davom etayotgan suhbatni topadi yoki yangisini ochadi.</summary>
    /// <param name="request">So'rov.</param>
    /// <param name="user">Foydalanuvchi.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Kuzatuvdagi suhbat (hali saqlanmagan bo'lishi mumkin).</returns>
    public async Task<AiConversation> OpenAsync(
        AiAskRequest request, AiUser user, CancellationToken cancellationToken)
    {
        DateTime since = DateTime.UtcNow - Window;

        AiConversation? existing = request.ConversationId is { } id
            ? await _db.AiConversations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            : request.TelegramChatId is { } chatId
                ? await _db.AiConversations
                    .Where(c => c.TelegramChatId == chatId && c.LastActivityAt >= since)
                    .OrderByDescending(c => c.LastActivityAt)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

        if (existing is not null)
        {
            existing.LastActivityAt = DateTime.UtcNow;
            return existing;
        }

        AiConversation conversation = new()
        {
            Channel = request.Channel,
            UserProfileId = user.UserProfileId,
            TelegramChatId = request.TelegramChatId,
            Language = user.Language,
            Title = Title(request.Text),
            LastActivityAt = DateTime.UtcNow,
        };

        _db.AiConversations.Add(conversation);
        return conversation;
    }

    /// <summary>Modelga beriladigan tarix.</summary>
    /// <param name="conversationId">Suhbat.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Xabarlar (eskisi birinchi), juftligi buzilmagan holda.</returns>
    public async Task<List<LlmMessage>> LoadHistoryAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        List<AiMessage> rows = await _db.AiMessages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.Sequence)
            .Take(HistoryLimit)
            .ToListAsync(cancellationToken);

        rows.Reverse();

        // Boshidan birinchi foydalanuvchi savoligacha tashlanadi (izohi sinf tavsifida).
        int start = rows.FindIndex(m => m.Role == AiMessageRole.User);
        if (start <= 0 && rows.Count > 0 && rows[0].Role != AiMessageRole.User)
        {
            return [];
        }

        return [.. rows.Skip(Math.Max(start, 0)).Select(ToLlmMessage)];
    }

    /// <summary>Suhbatdagi oxirgi tartib raqami.</summary>
    /// <param name="conversationId">Suhbat.</param>
    /// <param name="cancellationToken">Bekor qilish belgisi.</param>
    /// <returns>Oxirgi raqam; xabar bo'lmasa 0.</returns>
    public async Task<int> LastSequenceAsync(Guid conversationId, CancellationToken cancellationToken) =>
        await _db.AiMessages
            .Where(m => m.ConversationId == conversationId)
            .Select(m => (int?)m.Sequence)
            .MaxAsync(cancellationToken) ?? 0;

    /// <summary>Foydalanuvchi savolini qo'shadi.</summary>
    /// <param name="conversationId">Suhbat.</param>
    /// <param name="sequence">Tartib raqami.</param>
    /// <param name="text">Matn.</param>
    public void AddUser(Guid conversationId, int sequence, string text) =>
        _db.AiMessages.Add(new AiMessage
        {
            ConversationId = conversationId,
            Sequence = sequence,
            Role = AiMessageRole.User,
            Text = text,
        });

    /// <summary>Model javobini qo'shadi (tool chaqiriqlari va token hisobi bilan).</summary>
    /// <param name="conversationId">Suhbat.</param>
    /// <param name="sequence">Tartib raqami.</param>
    /// <param name="response">Model javobi.</param>
    public void AddAssistant(Guid conversationId, int sequence, LlmResponse response) =>
        _db.AiMessages.Add(new AiMessage
        {
            ConversationId = conversationId,
            Sequence = sequence,
            Role = AiMessageRole.Assistant,
            Text = response.Text,
            Model = response.Model,
            ToolCallsJson = response.ToolCalls.Count == 0
                ? null
                : JsonSerializer.Serialize(response.ToolCalls.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    arguments = c.Arguments,
                })),
            InputTokens = (int)response.Usage.InputTokens,
            OutputTokens = (int)response.Usage.OutputTokens,
            CacheWriteTokens = (int)response.Usage.CacheWriteTokens,
            CacheReadTokens = (int)response.Usage.CacheReadTokens,
        });

    /// <summary>Tool natijalarini qo'shadi.</summary>
    /// <param name="conversationId">Suhbat.</param>
    /// <param name="sequence">Tartib raqami.</param>
    /// <param name="results">Natijalar.</param>
    /// <remarks>
    /// ⚠️ Natijalar MIJOZ MA'LUMOTINI (qarz, narx, kontragent nomi) saqlaydi — jadval RLS
    /// ostida va tarix tozalash siyosati unga tegishli (<c>AiMessage</c> izohi).
    /// </remarks>
    public void AddToolResults(Guid conversationId, int sequence, IReadOnlyList<LlmToolResult> results) =>
        _db.AiMessages.Add(new AiMessage
        {
            ConversationId = conversationId,
            Sequence = sequence,
            Role = AiMessageRole.Tool,
            ToolResultsJson = JsonSerializer.Serialize(results.Select(r => new
            {
                toolCallId = r.ToolCallId,
                content = r.Content,
                isError = r.IsError,
            })),
        });

    /// <summary>Ro'yxatda ko'rinadigan sarlavha — birinchi savoldan.</summary>
    private static string Title(string text)
    {
        string trimmed = text.Trim();
        return trimmed.Length <= 60 ? trimmed : trimmed[..57] + "...";
    }

    /// <summary>Saqlangan xabarni provayder shakliga qaytaradi.</summary>
    /// <remarks>
    /// ⚠️ Tool chaqiriqlari va natijalari JSON'dan QAYTA tiklanadi: model o'z oldingi
    /// chaqirig'ini ko'rmasa, keyingi aylanishda o'shani takrorlab, bir xil ishni ikki
    /// marta qilardi.
    /// </remarks>
    private static LlmMessage ToLlmMessage(AiMessage message) => message.Role switch
    {
        AiMessageRole.User => LlmMessage.FromUser(message.Text ?? string.Empty),

        AiMessageRole.Assistant => LlmMessage.FromAssistant(message.Text, ReadToolCalls(message.ToolCallsJson)),

        _ => LlmMessage.FromToolResults(ReadToolResults(message.ToolResultsJson)),
    };

    private static IReadOnlyList<LlmToolCall> ReadToolCalls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using JsonDocument document = JsonDocument.Parse(json);
        return [.. document.RootElement.EnumerateArray().Select(e => new LlmToolCall(
            e.GetProperty("id").GetString() ?? string.Empty,
            e.GetProperty("name").GetString() ?? string.Empty,
            e.GetProperty("arguments").Clone()))];
    }

    private static IReadOnlyList<LlmToolResult> ReadToolResults(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using JsonDocument document = JsonDocument.Parse(json);
        return [.. document.RootElement.EnumerateArray().Select(e => new LlmToolResult(
            e.GetProperty("toolCallId").GetString() ?? string.Empty,
            e.GetProperty("content").GetString() ?? string.Empty,
            e.TryGetProperty("isError", out JsonElement error) && error.GetBoolean()))];
    }
}
