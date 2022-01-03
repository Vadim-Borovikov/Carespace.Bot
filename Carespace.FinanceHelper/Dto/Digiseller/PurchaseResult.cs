using Newtonsoft.Json;

namespace Carespace.FinanceHelper.Dto.Digiseller
{
    public sealed class PurchaseResult
    {
        public sealed class Content
        {
            [JsonProperty("promo_code")]
            public string PromoCode { get; set; }
        }

        [JsonProperty("content")]
        public Content Info { get; set; }
    }
}
