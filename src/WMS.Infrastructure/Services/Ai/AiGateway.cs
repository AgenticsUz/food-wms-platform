using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.Interfaces;
using WMS.Domain.Entities;
using WMS.Infrastructure.Persistence;

namespace WMS.Infrastructure.Services.Ai;

/// <inheritdoc />
public sealed class AiGateway : IAiGateway
{
    /// <summary>
    /// Sikl nechta aylanishi mumkin.
    /// </summary>
    /// <remarks>
    /// ⚠️ Chegara SHART: model tool chaqirishni to'xtatmasa (masalan natijani tushunmay
    /// qayta-qayta so'rasa), sikl cheksiz aylanib, bitta savol butun kunlik byudjetni
    /// yeb qo'yardi. Olti aylanish — «top → aniqlashtir → so'ra» zanjiriga yetarli.
    /// </remarks>
    private const int MaxTurns = 6;

    private readonly ILlmClient _llm;
    private readonly IAiToolRegistry _registry;
    private readonly IAiMetering _metering;
    private readonly ITenantStateService _tenantState;
    private readonly WmsDbContext _db;
    private readonly ILogger<AiGateway> _logger;

    /// <summary>
    /// Oxirgi oqimning jami sarfi — <see cref="AskAsync"/> uni javobga qo'shadi.
    /// </summary>
    /// <remarks>
    /// ⚠️ Gateway SCOPED (har so'rovga yangi nusxa), shuning uchun bu maydon bitta savolga
    /// tegishli. Hodisa oqimiga qo'shib yuborish mumkin edi, lekin token hisobi yuzaga
    /// KERAK EMAS — u faqat `ai_usage` va `AskAsync` chaqiruvchisi uchun.
    /// </remarks>
    private LlmUsage _lastUsage = LlmUsage.Empty;

    public AiGateway(
        ILlmClient llm,
        IAiToolRegistry registry,
        IAiMetering metering,
        ITenantStateService tenantState,
        WmsDbContext db,
        ILogger<AiGateway> logger)
    {
        _llm = llm;
        _registry = registry;
        _metering = metering;
        _tenantState = tenantState;
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AiAnswer> AskAsync(
        AiUser user, AiAskRequest request, CancellationToken cancellationToken = default)
    {
        // ⚠️ Sikl BITTA joyda: bu yerda faqat oqim yig'iladi. Ikki nusxa bo'lsa, qoidalar
        // qoidalar bir yuzada kuchga kirib, ikkinchisida jimgina yo'qolardi.
        List<AiToolOutput> tools = [];
        string text = string.Empty;
        Guid conversationId = Guid.Empty;

        await foreach (AiStreamEvent evt in StreamAsync(user, request, cancellationToken))
        {
            switch (evt.Type)
            {
                case AiStreamEvent.Types.Text:
                    text = evt.Text ?? string.Empty;
                    break;

                case AiStreamEvent.Types.ToolResult when evt.Tool is { } tool:
                    tools.Add(tool);
                    break;

                case AiStreamEvent.Types.Done:
                    conversationId = evt.ConversationId ?? Guid.Empty;
                    break;

                default:
                    break;
            }
        }

        return new AiAnswer(conversationId, text, tools, _lastUsage);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AiStreamEvent> StreamAsync(
        AiUser user,
        AiAskRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new AppException("Savol bo'sh.");
        }

        // ⚠️ Darvoza BIRINCHI hodisadan OLDIN: yuza SSE sarlavhalarini yozib bo'lgach
        // holat kodini o'zgartira olmaydi, ya'ni «AI o'chiq» 200 bo'lib ketardi.
        await _metering.EnsureAvailableAsync(cancellationToken);

        TenantState state = _db.CurrentTenantId is { } tenantId
            ? await _tenantState.GetAsync(tenantId, cancellationToken) ?? throw AiException.Disabled()
            : throw AiException.Disabled();

        AiToolContext toolContext = new(
            request.Channel,
            user.UserProfileId,
            user.Permissions,
            state.EnabledFeatures,
            user.Language);

        // ⚠️ Ro'yxat BIR MARTA olinadi va butun sikl davomida o'zgarmaydi: o'rtada qayta
        // hisoblansa kesh prefiksi (tools → system → messages) buzilib, keyingi
        // aylanishlar to'liq narxda ketardi.
        IReadOnlyList<IAiTool> available = _registry.Available(toolContext);
        IReadOnlyList<LlmToolDefinition> definitions =
            [.. available.Select(t => new LlmToolDefinition(t.Code, t.Description, t.Schema))];

        AiConversations conversations = new(_db);
        AiConversation conversation = await conversations.OpenAsync(request, user, cancellationToken);

        List<LlmMessage> messages = await conversations.LoadHistoryAsync(conversation.Id, cancellationToken);
        messages.Add(LlmMessage.FromUser(request.Text));

        int sequence = await conversations.LastSequenceAsync(conversation.Id, cancellationToken);
        conversations.AddUser(conversation.Id, ++sequence, request.Text);

        LlmUsage total = LlmUsage.Empty;
        string? answer = null;

        for (int turn = 0; turn < MaxTurns && answer is null; turn++)
        {
            LlmResponse response = await CompleteAsync(state, user, request, messages, definitions, cancellationToken);

            total = Add(total, response.Usage);
            await _metering.RecordAsync(response.Model, response.Usage, cancellationToken);

            conversations.AddAssistant(conversation.Id, ++sequence, response);
            messages.Add(LlmMessage.FromAssistant(response.Text, response.ToolCalls));

            if (response.ToolCalls.Count == 0)
            {
                answer = response.Text;
                break;
            }

            List<LlmToolResult> results = [];
            foreach (LlmToolCall call in response.ToolCalls)
            {
                (LlmToolResult result, AiToolOutput output) = await RunToolAsync(call, toolContext, cancellationToken);
                results.Add(result);

                // Tool tugagan zahoti yuzaga ketadi — foydalanuvchi ish borayotganini ko'rsin.
                yield return AiStreamEvent.OfTool(output);
            }

            conversations.AddToolResults(conversation.Id, ++sequence, results);
            messages.Add(LlmMessage.FromToolResults(results));
        }

        if (answer is null)
        {
            // Sikl chegaraga urildi. Bu NOSOZLIK emas, lekin jim o'tmaydi: modelga
            // berilgan tavsif yoki tool natijasi tushunarsiz ekanining belgisi.
            _logger.LogWarning(
                "AI sikli {MaxTurns} aylanishda tugamadi (suhbat {ConversationId})", MaxTurns, conversation.Id);

            answer = "So'rovni oxirigacha bajara olmadim. Savolni qisqaroq va aniqroq qilib bering.";
        }

        conversation.LastActivityAt = DateTime.UtcNow;
        await SaveAsync(cancellationToken);

        _lastUsage = total;

        yield return AiStreamEvent.OfText(answer);
        yield return AiStreamEvent.OfDone(conversation.Id);
    }

    /// <summary>
    /// Bitta provayder chaqirig'i.
    /// </summary>
    /// <remarks>
    /// Alohida metod, chunki <c>try/finally</c> iterator metodida <c>yield</c> bilan yonma-yon
    /// tura olmaydi — sikl esa oqim metodining o'zida qolishi kerak.
    /// </remarks>
    private async Task<LlmResponse> CompleteAsync(
        TenantState state,
        AiUser user,
        AiAskRequest request,
        List<LlmMessage> messages,
        IReadOnlyList<LlmToolDefinition> definitions,
        CancellationToken cancellationToken)
    {
        LlmRequest llmRequest = new()
        {
            SystemStable = AiSystemPrompt.Stable,
            SystemVolatile = AiSystemPrompt.Volatile(state.Name, user, request),

            // ⚠️ NUSXA, ro'yxatning o'zi emas: `messages` sikl davomida O'SADI va havola
            // berilsa, provayderga ketgan so'rov keyingi aylanishda jimgina «o'zgarib»
            // qolardi — qayta urinish, jurnal va test o'sha so'rovni BOSHQACHA ko'rardi.
            Messages = [.. messages],
            Tools = definitions,
        };

        try
        {
            return await _llm.CompleteAsync(llmRequest, cancellationToken);
        }
        finally
        {
            // Suhbat yarim qolsa ham yozilgani yoziladi: provayder javob bermaganda ham
            // token sarflangan bo'lishi mumkin va u hisobdan tushib qolmasin.
            await SaveAsync(cancellationToken);
        }
    }

    /// <summary>Bitta tool chaqirig'ini bajaradi.</summary>
    /// <remarks>
    /// ⚠️ Tool istisno ko'tarsa butun suhbat YIQILMAYDI: xato tool natijasi bo'lib modelga
    /// qaytadi va u boshqa yo'l tanlaydi yoki foydalanuvchiga tushunarli javob beradi.
    /// Istisno matni modelga UZATILMAYDI — unda ichki tafsilot (jadval nomi, so'rov) bo'lishi
    /// mumkin va u javob orqali foydalanuvchiga chiqib ketardi.
    /// </remarks>
    private async Task<(LlmToolResult Result, AiToolOutput Output)> RunToolAsync(
        LlmToolCall call, AiToolContext context, CancellationToken cancellationToken)
    {
        try
        {
            IAiTool tool = _registry.Require(call.Name, context);
            AiToolResult result = await tool.ExecuteAsync(call.Arguments, context, cancellationToken);

            return (
                new LlmToolResult(call.Id, result.Text, result.IsError),
                new AiToolOutput(call.Name, result.Text, result.Data));
        }
        catch (AiException ex)
        {
            // Ruxsat rad javobi — modelga tushunarli shaklda (qoida 5).
            string text = $"Bu amal bajarilmadi: {ex.Code}.";
            return (new LlmToolResult(call.Id, text, IsError: true), new AiToolOutput(call.Name, text, null));
        }
#pragma warning disable CA1031 // Tool nosozligi suhbatni uzmasligi kerak (izoh yuqorida).
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            _logger.LogError(ex, "AI tool '{Tool}' yiqildi", call.Name);

            const string text = "Bu ma'lumotni olishda texnik xato bo'ldi.";
            return (new LlmToolResult(call.Id, text, IsError: true), new AiToolOutput(call.Name, text, null));
        }
    }

    /// <summary>
    /// Suhbat tarixini yozadi.
    /// </summary>
    /// <remarks>
    /// ⚠️ Tool'lar O'ZLARI ham <c>SaveChangesAsync</c> chaqirishi mumkin emas (hammasi
    /// o'quvchi), shuning uchun bu yerda faqat AI jadvallari yoziladi. Yozuv har aylanishda
    /// qilinadi: uzun suhbat o'rtasida uzilib qolsa ham, savol va javob tarixda qoladi.
    /// </remarks>
    private Task SaveAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);

    private static LlmUsage Add(LlmUsage a, LlmUsage b) => new(
        a.InputTokens + b.InputTokens,
        a.OutputTokens + b.OutputTokens,
        a.CacheWriteTokens + b.CacheWriteTokens,
        a.CacheReadTokens + b.CacheReadTokens);
}
