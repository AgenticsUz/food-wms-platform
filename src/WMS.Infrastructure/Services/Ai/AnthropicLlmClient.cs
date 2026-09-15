using System.Globalization;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WMS.Application.Ai;
using WMS.Application.Common;

namespace WMS.Infrastructure.Services.Ai;

/// <summary>
/// <see cref="ILlmClient"/> ning Anthropic realizatsiyasi — Anthropic SDK turlarini
/// biladigan YAGONA sinf (F10 §0.9).
/// </summary>
/// <remarks>
/// <para>
/// Singleton: SDK klienti ichida <c>HttpClient</c> tutadi va uni har so'rovda qayta yaratish
/// ulanishlarni oqizadi (socket exhaustion).
/// </para>
/// <para>
/// ⚠️ Bu yerda qayta urinish YO'Q. SDK o'zi 429/5xx ni ikki marta qayta uradi, undan
/// tashqarisi gateway'ning ishi: sikl nechta aylanganini va qancha token ketganini faqat u
/// biladi. Qo'shimcha retry xarajatni jimgina ikki barobar qilardi.
/// </para>
/// </remarks>
public sealed class AnthropicLlmClient : ILlmClient
{
    private readonly AiOptions _options;
    private readonly ILogger<AnthropicLlmClient> _logger;
    private readonly AnthropicClient? _client;

    public AnthropicLlmClient(IOptions<AiOptions> options, ILogger<AnthropicLlmClient> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _logger = logger;

        // Kalit yo'q — klient ham yo'q. Sozlanmagan kalit bilan klient qurish startupda
        // emas, birinchi so'rovda yiqilardi (ya'ni foydalanuvchi oldida).
        _client = _options.IsConfigured
            ? new AnthropicClient
            {
                ApiKey = _options.ApiKey,
                Timeout = TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds)),
            }
            : null;
    }

    public bool IsConfigured => _client is not null;

    public string Model => _options.Model;

    public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_client is null)
        {
            throw AiException.Disabled();
        }

        MessageCreateParams parameters = new()
        {
            Model = _options.Model,
            MaxTokens = _options.MaxOutputTokens,
            System = BuildSystem(request),
            Messages = request.Messages.Select(ToMessageParam).ToList(),
            Tools = request.Tools.Select(d => new ToolUnion(ToTool(d))).ToList(),

            // Sonnet 5 da `thinking` adaptiv rejimdan boshqasini QABUL QILMAYDI: eski
            // `budget_tokens` 400 qaytaradi. Chuqurlikni `effort` boshqaradi.
            Thinking = new ThinkingConfigAdaptive(),
            OutputConfig = new OutputConfig { Effort = ParseEffort(_options.Effort) },
        };

        Message response;
        try
        {
            response = await _client.Messages.Create(parameters, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Provayder nosozligi mijozga 503 bo'lib chiqadi; tafsilot faqat logda —
            // xato matnida so'rov qismlari bo'lishi mumkin.
            _logger.LogError(ex, "Anthropic chaqirig'i yiqildi (model {Model})", _options.Model);
            throw AiException.Unavailable();
        }

        return MapResponse(response);
    }

    /// <summary>
    /// System prompt IKKI blok: barqaror (kesh chegarasi bilan) va o'zgaruvchan.
    /// </summary>
    /// <remarks>
    /// Kesh prefiksi <c>tools → system → messages</c> tartibida yig'iladi, ya'ni chegaradan
    /// OLDINGI hamma narsa keshlanadi. Sana yoki tenant nomi barqaror blokka tushsa prefiks
    /// har so'rovda o'zgarib, kesh hech qachon urmasdi.
    /// </remarks>
    private static List<TextBlockParam> BuildSystem(LlmRequest request)
    {
        List<TextBlockParam> blocks =
        [
            new() { Text = request.SystemStable, CacheControl = new CacheControlEphemeral() },
        ];

        if (!string.IsNullOrWhiteSpace(request.SystemVolatile))
        {
            blocks.Add(new TextBlockParam { Text = request.SystemVolatile });
        }

        return blocks;
    }

    private static Tool ToTool(LlmToolDefinition definition) => new()
    {
        Name = definition.Name,
        Description = definition.Description,

        // Sxema TO'LIQ uzatiladi (`properties`/`required` ga bo'lib emas): ichma-ich
        // obyekt va massivli sxemalar bo'linganda jimgina buzilardi.
        InputSchema = InputSchema.FromRawUnchecked(ToDictionary(definition.Schema)),
    };

    private static MessageParam ToMessageParam(LlmMessage message)
    {
        if (message.Role == LlmRole.User && message.ToolResults.Count > 0)
        {
            return new MessageParam
            {
                Role = Role.User,
                Content = message.ToolResults
                    .Select(r => (ContentBlockParam)new ToolResultBlockParam
                    {
                        ToolUseID = r.ToolCallId,
                        Content = r.Content,
                        IsError = r.IsError,
                    })
                    .ToList(),
            };
        }

        if (message.Role == LlmRole.User)
        {
            return new MessageParam { Role = Role.User, Content = message.Text ?? string.Empty };
        }

        List<ContentBlockParam> content = [];
        if (!string.IsNullOrWhiteSpace(message.Text))
        {
            content.Add(new TextBlockParam { Text = message.Text });
        }

        foreach (LlmToolCall call in message.ToolCalls)
        {
            content.Add(new ToolUseBlockParam
            {
                ID = call.Id,
                Name = call.Name,
                Input = ToDictionary(call.Arguments),
            });
        }

        return new MessageParam { Role = Role.Assistant, Content = content };
    }

    private static LlmResponse MapResponse(Message response)
    {
        string? text = null;
        List<LlmToolCall> calls = [];

        foreach (ContentBlock block in response.Content)
        {
            if (block.TryPickText(out TextBlock? textBlock))
            {
                // Bir nechta matn bloki bo'lsa birlashtiriladi — modelning bir gapini
                // ikkinchisi bilan almashtirib yuborish javobni buzardi.
                text = text is null ? textBlock.Text : text + "\n" + textBlock.Text;
            }
            else if (block.TryPickToolUse(out ToolUseBlock? toolUse))
            {
                calls.Add(new LlmToolCall(
                    toolUse.ID,
                    toolUse.Name,
                    JsonSerializer.SerializeToElement(toolUse.Input)));
            }
        }

        return new LlmResponse(
            text,
            calls,
            MapStopReason(response.StopReason?.ToString(), calls.Count > 0),
            MapUsage(response.Usage),
            response.Model.ToString() ?? string.Empty);
    }

    /// <summary>
    /// To'xtash sababini normallashtiradi.
    /// </summary>
    /// <remarks>
    /// ⚠️ Satr shakli ataylab ikki ko'rinishda qabul qilinadi (<c>tool_use</c> va
    /// <c>ToolUse</c>): SDK enum'ining <c>ToString()</c> i sim shakli ham, C# nomi ham
    /// bo'lishi mumkin va noto'g'ri taxmin qilinsa sikl «javob tugadi» deb yarim joyda
    /// to'xtardi. Tool chaqirig'i BORLIGI esa — shubhasiz belgi, shuning uchun u ustun.
    /// </remarks>
    private static LlmStopReason MapStopReason(string? raw, bool hasToolCalls)
    {
        if (hasToolCalls)
        {
            return LlmStopReason.ToolUse;
        }

        string normalized = (raw ?? string.Empty).Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
        return normalized switch
        {
            "endturn" => LlmStopReason.EndTurn,
            "tooluse" => LlmStopReason.ToolUse,
            "maxtokens" => LlmStopReason.MaxTokens,
            "refusal" => LlmStopReason.Refusal,
            _ => LlmStopReason.Other,
        };
    }

    private static LlmUsage MapUsage(Usage? usage) => usage is null
        ? LlmUsage.Empty
        : new LlmUsage(
            usage.InputTokens,
            usage.OutputTokens,
            usage.CacheCreationInputTokens ?? 0,
            usage.CacheReadInputTokens ?? 0);

    private static Effort ParseEffort(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "low" => Effort.Low,
            "medium" => Effort.Medium,
            "high" => Effort.High,
            "xhigh" => Effort.Xhigh,
            "max" => Effort.Max,

            // Noma'lum qiymat jimgina sukutga tushmaydi: konfigdagi terish xatosi
            // xarajatni bir necha barobar oshirishi mumkin edi.
            _ => throw new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture,
                "'Ai:Effort' qiymati noto'g'ri: '{0}'. Ruxsat etilgan: low, medium, high, xhigh, max.",
                value)),
        };

    private static Dictionary<string, JsonElement> ToDictionary(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
            ? element.EnumerateObject().ToDictionary(p => p.Name, p => p.Value, StringComparer.Ordinal)
            : [];

    private static Dictionary<string, JsonElement> ToDictionary(IReadOnlyDictionary<string, JsonElement> source) =>
        source.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
}
