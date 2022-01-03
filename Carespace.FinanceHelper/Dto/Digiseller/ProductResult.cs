using Newtonsoft.Json;

namespace Carespace.FinanceHelper.Dto.Digiseller
{
    public sealed class ProductResult
    {
        public sealed class Product
        {
            [JsonProperty]
            public string Name { get; set; }
        }

        [JsonProperty("product")]
        public Product Info { get; set; }
    }
}
