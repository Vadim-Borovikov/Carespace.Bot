using JetBrains.Annotations;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Carespace.FinanceHelper.Data.Digiseller;

internal sealed class SellsRequest
{
    [UsedImplicitly]
    [JsonPropertyName("id_seller")]
    public int? SellerId;

    [UsedImplicitly]
    public List<int?>? ProductIds;

    [UsedImplicitly]
    public string? DateStart;

    [UsedImplicitly]
    public string? DateFinish;

    [UsedImplicitly]
    public int? Returned;

    [UsedImplicitly]
    public int? Rows;

    [UsedImplicitly]
    public int? Page;

    [UsedImplicitly]
    public string? Sign;
}