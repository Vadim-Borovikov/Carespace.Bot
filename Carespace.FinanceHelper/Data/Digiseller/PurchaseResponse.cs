using Newtonsoft.Json;

namespace Carespace.FinanceHelper.Data.Digiseller;

public sealed class PurchaseResponse
{
    public sealed class ContentInfo
    {
        [JsonProperty]
        public string? PromoCode { get; set; }
    }

    [JsonProperty]
    public ContentInfo? Content { get; set; }
}