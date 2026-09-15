using WMS.Application.Ai;

namespace WMS.Tests.Ai;

/// <summary>
/// Testdagi <see cref="ILlmClient"/> — tarmoqqa CHIQMAYDI.
/// </summary>
/// <remarks>
/// <para>
/// F10 §0.10: testlar API'siz yuradi. Jonli chaqiriq bo'lsa to'plam sekin, qimmat va
/// beqaror bo'lardi — model bir xil savolga har safar boshqacha javob beradi.
/// </para>
/// <para>
/// Javoblar navbatga oldindan qo'yiladi: gateway sikli (A1) bir so'rovda bir necha marta
/// chaqiradi, ya'ni «bitta javob» yetarli emas. Navbat bo'shasa test AYNAN shu yerda
/// yiqiladi — jimgina bo'sh javob qaytarilsa, sabab keyingi assert'da izlanardi.
/// </para>
/// </remarks>
public sealed class FakeLlmClient : ILlmClient
{
    private readonly Queue<LlmResponse> _responses = new();

    /// <summary>Qabul qilingan so'rovlar — tool ro'yxati va prompt shu yerdan tekshiriladi.</summary>
    public List<LlmRequest> Requests { get; } = [];

    /// <inheritdoc />
    public bool IsConfigured { get; set; } = true;

    /// <inheritdoc />
    public string Model { get; set; } = "claude-sonnet-5";

    /// <summary>Navbatga javob qo'yadi.</summary>
    /// <param name="response">Qaytariladigan javob.</param>
    /// <returns>O'zi (zanjirlab yozish uchun).</returns>
    public FakeLlmClient Enqueue(LlmResponse response)
    {
        _responses.Enqueue(response);
        return this;
    }

    /// <summary>Oddiy matnli javob qo'yadi.</summary>
    /// <param name="text">Javob matni.</param>
    /// <param name="usage">Token hisobi; berilmasa nol.</param>
    /// <returns>O'zi.</returns>
    public FakeLlmClient EnqueueText(string text, LlmUsage? usage = null) =>
        Enqueue(new LlmResponse(text, [], LlmStopReason.EndTurn, usage ?? LlmUsage.Empty, Model));

    /// <summary>Testlar orasida holatni tozalaydi.</summary>
    public void Reset()
    {
        _responses.Clear();
        Requests.Clear();
        IsConfigured = true;
    }

    /// <inheritdoc />
    public Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);

        return _responses.Count > 0
            ? Task.FromResult(_responses.Dequeue())
            : throw new InvalidOperationException(
                "FakeLlmClient navbati bo'sh: test kutilgan javoblarni oldindan qo'yishi kerak.");
    }
}
