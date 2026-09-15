using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.Common.Localization;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers.Ai;

/// <summary>Web panelidan kelgan savol.</summary>
/// <param name="Text">Foydalanuvchi matni.</param>
/// <param name="ConversationId">Davom etayotgan suhbat; <see langword="null"/> — yangisi.</param>
/// <param name="PageContext">Joriy sahifa va tanlangan ombor (ixtiyoriy).</param>
public sealed record AiChatRequest(string Text, Guid? ConversationId = null, string? PageContext = null);

/// <summary>
/// AI yordamchisi (<c>/api/ai</c>).
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>Bu yerda <c>[RequirePermission]</c> ATAYLAB yo'q.</b> AI'ning chegarasi — tool
/// registri: foydalanuvchi ruxsatiga kirmagan amal modelga umuman ko'rsatilmaydi (F10 §0.3).
/// Controllerga bitta ruxsat qo'yish yo yolg'on tuyg'u berardi (bitta ruxsat bilan hamma
/// tool ochilgandek), yo AI'ni ruxsati tor xodimlarga butunlay yopardi.
/// </para>
/// <para>
/// ⚠️ <c>[RequireModule]</c> ham yo'q: AI bitta modulga tegishli emas, uning o'chirgichi —
/// <c>ai.chat</c> feature'i (gateway tekshiradi).
/// </para>
/// </remarks>
[Route("api/ai")]
public sealed class AiController : BaseController
{
    /// <summary>SSE hodisalari mijoz kutgan shaklda (camelCase).</summary>
    private static readonly JsonSerializerOptions EventOptions = new(JsonSerializerDefaults.Web);

    private readonly IAiGateway _gateway;
    private readonly IAiHistory _history;
    private readonly IRequestLanguage _language;

    /// <summary>Servislarni oladi.</summary>
    /// <param name="gateway">AI oqimi.</param>
    /// <param name="history">Suhbat tarixi.</param>
    /// <param name="language">So'rov tili.</param>
    public AiController(IAiGateway gateway, IAiHistory history, IRequestLanguage language)
    {
        _gateway = gateway;
        _history = history;
        _language = language;
    }

    /// <summary>
    /// Savol → javob, SSE hodisalari bilan.
    /// </summary>
    /// <param name="request">Savol.</param>
    /// <param name="cancellationToken">So'rov uzilsa.</param>
    /// <returns>Asinxron amal (javob to'g'ridan-to'g'ri oqimga yoziladi).</returns>
    /// <remarks>
    /// <para>
    /// ⚠️ <c>text/event-stream</c>, lekin <c>EventSource</c> EMAS: brauzerning
    /// <c>EventSource</c>'i <c>Authorization</c> sarlavhasini yubora olmaydi. Mijoz
    /// <c>fetch</c> + <c>ReadableStream</c> bilan o'qiydi.
    /// </para>
    /// <para>
    /// ⚠️ Rad javobi (AI o'chiq, kvota) BIRINCHI hodisadan oldin keladi va oddiy JSON
    /// bo'lib chiqadi: sarlavhalar yozilgandan keyin holat kodini o'zgartirib bo'lmaydi
    /// va «AI o'chiq» 200 bo'lib ketardi. Oqim boshlangandan keyingi nosozlik esa
    /// <c>error</c> hodisasi bo'ladi.
    /// </para>
    /// </remarks>
    [HttpPost("chat")]
    public async Task Chat([FromBody] AiChatRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        AiUser user = new(UserId, CurrentUser.Permissions, _language.Current);
        AiAskRequest ask = new(
            AiChannel.Web, request.Text, request.ConversationId, PageContext: request.PageContext);

        bool started = false;
        try
        {
            await foreach (AiStreamEvent evt in _gateway.StreamAsync(user, ask, cancellationToken))
            {
                if (!started)
                {
                    StartStream();
                    started = true;
                }

                await WriteAsync(evt, cancellationToken);
            }
        }
        catch (AiException ex) when (started)
        {
            await WriteAsync(
                AiStreamEvent.OfError(ex.Code, Translations.Format(ex.MessageTemplate, _language.Current, ex.MessageArgs)),
                cancellationToken);
        }
    }

    /// <summary>Foydalanuvchining suhbatlari.</summary>
    /// <param name="cancellationToken">So'rov uzilsa.</param>
    /// <returns>Suhbatlar ro'yxati.</returns>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken) =>
        Ok(ApiResponse<IReadOnlyList<AiConversationDto>>.Ok(
            await _history.ListAsync(UserId, cancellationToken: cancellationToken)));

    /// <summary>Bitta suhbat va uning xabarlari.</summary>
    /// <param name="id">Suhbat.</param>
    /// <param name="cancellationToken">So'rov uzilsa.</param>
    /// <returns>Suhbat; boshqanikiga tegishli bo'lsa 404.</returns>
    /// <remarks>
    /// Begona suhbatga 404 (403 emas): «bunday suhbat bor, lekin sizga ko'rinmaydi» degan
    /// javobning o'zi ham ma'lumot.
    /// </remarks>
    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> GetConversation(Guid id, CancellationToken cancellationToken)
    {
        AiConversationDetailDto? conversation = await _history.GetAsync(UserId, id, cancellationToken);

        return conversation is null
            ? NotFound(ApiResponse<AiConversationDetailDto>.Fail("Conversation not found"))
            : Ok(ApiResponse<AiConversationDetailDto>.Ok(conversation));
    }

    /// <summary>SSE sarlavhalari — birinchi hodisadan oldin bir marta.</summary>
    /// <remarks>
    /// <c>X-Accel-Buffering: no</c> — nginx oqimni BUFERLAMASIN: aks holda hamma hodisa
    /// javob tugagandan keyin birdan kelib, oqimning ma'nosi qolmasdi.
    /// </remarks>
    private void StartStream()
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";
    }

    private async Task WriteAsync(AiStreamEvent evt, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(evt, EventOptions);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
