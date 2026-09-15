using System.Text.Json;
using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;

namespace WMS.API.Controllers.Ai;

/// <summary>
/// AI oqimini SSE bo'lib yozadi — ilova yuzasi ham, kabinet ham shundan foydalanadi.
/// </summary>
/// <remarks>
/// <para>
/// Nega umumiy: ikki controller bir xil ishni qiladi va nusxa bo'lsa, «rad javobi
/// birinchi hodisadan oldin» kabi nozik qoida bir yuzada tuzatilib, ikkinchisida
/// eski holida qolib ketardi.
/// </para>
/// <para>
/// ⚠️ Rad javobi (AI o'chiq, kvota) BIRINCHI hodisadan oldin keladi va oddiy JSON
/// bo'lib chiqadi — sarlavhalar yozilgandan keyin holat kodini o'zgartirib bo'lmaydi
/// va «AI o'chiq» 200 bo'lib ketardi. Oqim boshlangandan keyingi nosozlik esa
/// <c>error</c> hodisasi bo'ladi.
/// </para>
/// </remarks>
internal static class AiSseWriter
{
    /// <summary>SSE hodisalari mijoz kutgan shaklda (camelCase).</summary>
    private static readonly JsonSerializerOptions EventOptions = new(JsonSerializerDefaults.Web);

    public static async Task WriteAsync(
        HttpResponse response,
        IAiGateway gateway,
        AiUser user,
        AiAskRequest request,
        IRequestLanguage language,
        CancellationToken cancellationToken)
    {
        bool started = false;

        try
        {
            await foreach (AiStreamEvent evt in gateway.StreamAsync(user, request, cancellationToken))
            {
                if (!started)
                {
                    Start(response);
                    started = true;
                }

                await SendAsync(response, evt, cancellationToken);
            }
        }
        catch (AiException ex) when (started)
        {
            await SendAsync(
                response,
                AiStreamEvent.OfError(ex.Code, Translations.Format(ex.MessageTemplate, language.Current, ex.MessageArgs)),
                cancellationToken);
        }
    }

    /// <summary>
    /// SSE sarlavhalari — birinchi hodisadan oldin bir marta.
    /// </summary>
    /// <remarks>
    /// <c>X-Accel-Buffering: no</c> — nginx oqimni BUFERLAMASIN: aks holda hamma hodisa
    /// javob tugagandan keyin birdan kelib, oqimning ma'nosi qolmasdi.
    /// </remarks>
    private static void Start(HttpResponse response)
    {
        response.Headers.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";
    }

    private static async Task SendAsync(HttpResponse response, AiStreamEvent evt, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(evt, EventOptions);
        await response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
