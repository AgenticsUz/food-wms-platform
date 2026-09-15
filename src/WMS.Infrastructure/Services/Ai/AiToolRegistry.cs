using Microsoft.Extensions.Logging;
using WMS.Application.Ai;
using WMS.Application.Common;

namespace WMS.Infrastructure.Services.Ai;

/// <inheritdoc />
public sealed class AiToolRegistry : IAiToolRegistry
{
    private readonly IReadOnlyDictionary<string, IAiTool> _tools;
    private readonly ILogger<AiToolRegistry> _logger;

    /// <summary>DI'dagi hamma tool'ni yig'adi.</summary>
    /// <param name="tools">Ro'yxatdan o'tgan tool'lar.</param>
    /// <param name="logger">Jurnal.</param>
    /// <exception cref="InvalidOperationException">Ikki tool bir xil kod bilan ro'yxatdan o'tgan.</exception>
    /// <remarks>
    /// ⚠️ Takror kod STARTUPDA yiqitadi. Jimgina «oxirgisi yutadi» qoidasi bo'lsa, modelga
    /// ko'rinadigan nom ortida kutilmagan amal turishi mumkin edi — bu ruxsat chegarasini
    /// aylanib o'tishning eng qisqa yo'li.
    /// </remarks>
    public AiToolRegistry(IEnumerable<IAiTool> tools, ILogger<AiToolRegistry> logger)
    {
        ArgumentNullException.ThrowIfNull(tools);

        Dictionary<string, IAiTool> map = new(StringComparer.Ordinal);
        foreach (IAiTool tool in tools)
        {
            if (!map.TryAdd(tool.Code, tool))
            {
                throw new InvalidOperationException($"AI tool kodi takrorlandi: '{tool.Code}'.");
            }
        }

        _tools = map;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<IAiTool> Available(AiToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return [.. _tools.Values.Where(t => IsAllowed(t, context)).OrderBy(t => t.Code, StringComparer.Ordinal)];
    }

    /// <inheritdoc />
    public IAiTool Require(string code, AiToolContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_tools.TryGetValue(code ?? string.Empty, out IAiTool? tool) || !IsAllowed(tool, context))
        {
            // Model ko'rmagan tool'ni chaqirdi — buni ko'rish kerak: yo ro'yxat va bajaruvchi
            // ayrilgan, yo model nomni o'zi to'qigan.
            _logger.LogWarning("AI ruxsat etilmagan tool'ni chaqirdi: {Tool}", code);
            throw AiException.ToolForbidden(code ?? "?");
        }

        return tool;
    }

    /// <summary>
    /// Ruxsat SHARTI: ruxsat kodi ham, feature ham bo'lishi kerak.
    /// </summary>
    /// <remarks>
    /// Ikkisi BOSHQA savolga javob beradi: ruxsat — «shu odam ko'ra oladimi», feature —
    /// «tenant buni sotib olganmi». Bittasi bilan cheklansa, feature o'chirilgan modulning
    /// ma'lumoti admin roli orqali AI'da ko'rinib qolardi.
    /// </remarks>
    private static bool IsAllowed(IAiTool tool, AiToolContext context) =>
        context.HasPermission(tool.PermissionCode) && context.HasFeature(tool.FeatureCode);
}
