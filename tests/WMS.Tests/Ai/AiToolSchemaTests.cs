using System.Text.Json;
using WMS.Application.Ai;
using WMS.Domain.Enums;

namespace WMS.Tests.Ai;

/// <summary>
/// Tool argumentlari sxemasi DTO'dan chiqadi va o'sha sozlamalar bilan qayta o'qiladi.
/// </summary>
/// <remarks>
/// Eng muhim da'vo — SXEMA va O'QISH bir xil nomlashda. Ular ayrilsa model sxemadagi
/// nomni yuborardi, o'qish esa boshqa nomni kutardi: maydon jimgina bo'sh qolib,
/// «AI parametrni e'tiborsiz qoldiryapti» degan tushunarsiz nuqson chiqardi.
/// </remarks>
public sealed class AiToolSchemaTests
{
    [Fact]
    public void Sxema_DTO_dan_camelCase_nomlar_bilan_chiqadi()
    {
        JsonElement schema = AiToolSchema.For<StockQueryArgs>();

        schema.GetProperty("type").GetString().ShouldBe("object");

        JsonElement properties = schema.GetProperty("properties");
        properties.EnumerateObject().Select(p => p.Name)
            .ShouldBe(["productName", "warehouseName", "limit"], ignoreOrder: true);
    }

    [Fact]
    public void Argumentsiz_tool_sxemasi_ham_object()
    {
        AiToolSchema.Empty.GetProperty("type").GetString().ShouldBe("object");
    }

    [Fact]
    public void Model_yuborgan_argumentlar_oqiladi()
    {
        JsonElement arguments = Json("""{"productName":"Snikers","limit":5}""");

        AiToolSchema.TryRead(arguments, out StockQueryArgs args, out string? error).ShouldBeTrue();

        error.ShouldBeNull();
        args.ProductName.ShouldBe("Snikers");
        args.Limit.ShouldBe(5);
        args.WarehouseName.ShouldBeNull();
    }

    [Fact]
    public void Enum_nomi_bilan_oqiladi()
    {
        // Model raqam emas, NOM yuboradi — sxemada ham shunday e'lon qilingan.
        JsonElement arguments = Json("""{"channel":"telegram"}""");

        AiToolSchema.TryRead(arguments, out ChannelArgs args, out _).ShouldBeTrue();
        args.Channel.ShouldBe(AiChannel.Telegram);
    }

    [Fact]
    public void Sxemani_buzgan_argument_istisno_KOTARMAYDI()
    {
        // `limit` — son, model esa matn yubordi. Bu KUTILGAN holat: sabab modelga tool
        // natijasi bo'lib qaytadi va u o'zini tuzatib qayta chaqiradi.
        JsonElement arguments = Json("""{"limit":"besh"}""");

        AiToolSchema.TryRead(arguments, out StockQueryArgs _, out string? error).ShouldBeFalse();
        error.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Bosh_argument_sukut_qiymatlarni_beradi()
    {
        AiToolSchema.TryRead(default, out StockQueryArgs args, out _).ShouldBeTrue();
        args.ProductName.ShouldBeNull();
    }

    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement;

    /// <summary>A1 dagi <c>stock_query</c> argumentlarining shakli.</summary>
    private sealed class StockQueryArgs
    {
        public string? ProductName { get; set; }

        public string? WarehouseName { get; set; }

        public int Limit { get; set; }
    }

    private sealed class ChannelArgs
    {
        public AiChannel Channel { get; set; }
    }
}
