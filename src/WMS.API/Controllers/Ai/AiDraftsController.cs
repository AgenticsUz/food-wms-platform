using Microsoft.AspNetCore.Mvc;
using WMS.API.Middleware;
using WMS.Application.Ai;
using WMS.Application.Common;
using WMS.Application.DTOs.Finance;
using WMS.Application.DTOs.Transfers;
using WMS.Application.Interfaces;
using WMS.Domain.Enums;

namespace WMS.API.Controllers.Ai;

/// <summary>AI qoralamasidan hujjat yaratish so'rovi.</summary>
/// <param name="Draft">Foydalanuvchi ko'rgan (va tahrirlagan) qoralama.</param>
/// <param name="ConversationId">Qoralamani tayyorlagan suhbat — audit izi.</param>
public sealed record CreateFromTransferDraftRequest(AiTransferDraft Draft, Guid? ConversationId = null);

/// <summary>AI qoralamasidan to'lov yozish so'rovi.</summary>
/// <param name="Draft">Foydalanuvchi ko'rgan qoralama.</param>
/// <param name="ConversationId">Qoralamani tayyorlagan suhbat.</param>
public sealed record CreateFromPaymentDraftRequest(AiPaymentDraft Draft, Guid? ConversationId = null);

/// <summary>
/// AI qoralamalarini yozuvga aylantirish (<c>/api/ai/drafts</c>) — ODAM tugmasi.
/// </summary>
/// <remarks>
/// <para>
/// ⚠️ <b>Nega alohida controller, nega AI o'zi yozmaydi</b> (F10 §0.6): yozuvchi amal
/// faqat odam tasdig'i bilan bo'ladi. AI qatlamida <c>confirm_transfer</c> yoki
/// <c>create_payment</c> tool'i YO'Q — model bu manzillarni ko'rmaydi ham, chaqira ham
/// olmaydi. Bu yerga faqat brauzerdagi tugma keladi.
/// </para>
/// <para>
/// ⚠️ <c>Source = Ai</c> ni SERVER qo'yadi, mijoz emas: aks holda oddiy forma ham o'zini
/// «AI yozgan» deb ko'rsatib, audit izini bulg'ashi mumkin edi. Shu sababli qoralama
/// yuk tarkibida kelsa ham, manba va suhbat id'si shu yerda hal bo'ladi.
/// </para>
/// <para>
/// ⚠️ Hujjat <c>Pending</c> bo'lib yaratiladi. Tasdiqlash (zaxirani kamaytiradigan qadam)
/// eski joyida — <c>TransfersController.Confirm</c>: AI oqimi uni qisqartirmaydi.
/// </para>
/// </remarks>
[Route("api/ai/drafts")]
public sealed class AiDraftsController : BaseController
{
    private readonly ITransferService _transfers;
    private readonly IFinanceService _finance;

    /// <summary>Servislarni oladi.</summary>
    /// <param name="transfers">Hujjat servisi.</param>
    /// <param name="finance">Moliya servisi.</param>
    public AiDraftsController(ITransferService transfers, IFinanceService finance)
    {
        _transfers = transfers;
        _finance = finance;
    }

    /// <summary>
    /// Qoralamadan hujjat yaratadi (<c>Pending</c>).
    /// </summary>
    /// <param name="request">Qoralama va suhbat.</param>
    /// <returns>Yaratilgan hujjat.</returns>
    /// <remarks>
    /// ⚠️ Qoralama ESKIRGAN bo'lishi mumkin: u tayyorlangandan keyin qoldiq kamaygan yoki
    /// narx o'zgargan bo'lishi mumkin. Shuning uchun bu yerda hech narsa «ishonch bilan»
    /// olinmaydi — hamma tekshiruv <c>TransferService.CreateAsync</c> ichida qayta
    /// yuradi (qoldiq, feature, sana ruxsati, plan limiti) va yetmasa xato qaytadi.
    /// </remarks>
    [RequirePermission(WmsPermissions.TransfersCreate)]
    [RequireModule(ModuleCodes.Transfers)]
    [HttpPost("transfer")]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateFromTransferDraftRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        AiTransferDraft draft = request.Draft;

        CreateTransferDto dto = new()
        {
            Type = draft.Type,
            DocumentDate = draft.DocumentDate,
            CounterpartyId = draft.CounterpartyId,
            Note = draft.Note,

            // Kirim — omborga, chiqim — ombordan. Qoralamada bitta ombor bor, chunki
            // AI ichki ko'chirish qoralamasini tayyorlamaydi.
            ToWarehouseId = draft.Type == TransferType.Incoming ? draft.WarehouseId : null,
            FromWarehouseId = draft.Type == TransferType.Outgoing ? draft.WarehouseId : null,

            Items = [.. draft.Items.Select(i => new CreateTransferItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
            })],
        };

        TransferDto created = await _transfers.CreateAsync(
            UserId, dto, DocumentSource.Ai, request.ConversationId);

        return Ok(ApiResponse<TransferDto>.Ok(created));
    }

    /// <summary>
    /// Qoralamadan to'lov yozuvini kiritadi.
    /// </summary>
    /// <param name="request">Qoralama va suhbat.</param>
    /// <returns>Yozilgan to'lov.</returns>
    /// <remarks>
    /// ⚠️ Yo'nalish qoralamadan OSHKORA uzatiladi (<c>Direction</c> to'ldirilgan holda):
    /// bo'sh qoldirilsa servis uni qarz belgisidan chiqarardi va nol balansda summa
    /// teskari tomonga yozilishi mumkin edi (`CreatePaymentDto.Direction` izohi).
    /// </remarks>
    [RequirePermission(WmsPermissions.FinanceManage)]
    [RequireModule(ModuleCodes.Finance)]
    [HttpPost("payment")]
    public async Task<IActionResult> CreatePayment([FromBody] CreateFromPaymentDraftRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        AiPaymentDraft draft = request.Draft;

        CreatePaymentDto dto = new()
        {
            CounterpartyId = draft.CounterpartyId,
            Amount = draft.Amount,
            Direction = draft.Direction,
            Method = draft.Method,
            DocumentDate = draft.DocumentDate,
            Note = draft.Note,
        };

        PaymentHistoryDto created = await _finance.CreatePaymentAsync(
            UserId, dto, DocumentSource.Ai, request.ConversationId);

        return Ok(ApiResponse<PaymentHistoryDto>.Ok(created));
    }
}
