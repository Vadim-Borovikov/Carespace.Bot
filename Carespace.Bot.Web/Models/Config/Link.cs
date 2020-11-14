using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models.Config
{
    public sealed class Link
    {
        [JsonProperty]
        public string Name { get; set; }
        [JsonProperty]
        public string Url { get; set; }
        [JsonProperty]
        public string PhotoPath { get; set; }
    }
}