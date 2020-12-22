using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models.Config
{
    public sealed class Account
    {
        [JsonProperty]
        public string BankId { get; set; }
        [JsonProperty]
        public string CardNumber { get; set; }
    }
}