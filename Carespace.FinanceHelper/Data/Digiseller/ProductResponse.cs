using Newtonsoft.Json;

namespace Carespace.FinanceHelper.Data.Digiseller;

public sealed class ProductResponse
{
    public sealed class ProductInfo
    {
        [JsonProperty]
        public string? Name { get; set; }
    }

    [JsonProperty]
    public ProductInfo? Product { get; set; }
}