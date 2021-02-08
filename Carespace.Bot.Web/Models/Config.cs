using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models
{
    public sealed class Config : Carespace.Bot.Config.Config
    {
        [JsonProperty]
        public string GoogleCredentialJson { get; set; }

        [JsonProperty]
        public string AdminIdsJson { get; set; }

        [JsonProperty]
        public string CultureInfoName { get; set; }
    }
}