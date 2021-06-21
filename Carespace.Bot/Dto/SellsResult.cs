using System.Collections.Generic;
using Newtonsoft.Json;

namespace Carespace.Bot.Dto
{
    public sealed class SellsResult
    {
        public sealed class Sell
        {
            [JsonProperty]
            public string Email { get; set; }
        }

        [JsonProperty]
        public int Pages { get; set; }
        [JsonProperty("rows")]
        public List<Sell> Sells { get; set; }
    }
}
