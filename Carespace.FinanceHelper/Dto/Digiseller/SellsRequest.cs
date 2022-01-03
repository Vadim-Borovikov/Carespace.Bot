using System.Collections.Generic;
using Newtonsoft.Json;

namespace Carespace.FinanceHelper.Dto.Digiseller
{
    public sealed class SellsRequest
    {
        [JsonProperty("id_seller")]
        public int SellerId { get; set; }
        [JsonProperty]
        public List<int> ProductIds { get; set; }
        [JsonProperty]
        public string DateStart { get; set; }
        [JsonProperty]
        public string DateFinish { get; set; }
        [JsonProperty]
        public int Returned { get; set; }
        [JsonProperty]
        public int Rows { get; set; }
        [JsonProperty]
        public int Page { get; set; }
        [JsonProperty]
        public string Sign { get; set; }
    }
}
