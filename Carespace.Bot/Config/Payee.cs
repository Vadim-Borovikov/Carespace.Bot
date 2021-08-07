using System.Collections.Generic;
using Newtonsoft.Json;

namespace Carespace.Bot.Config
{
    public sealed class Payee1
    {
        [JsonProperty]
        public string Name { get; set; }
        [JsonProperty]
        public string PhotoPath { get; set; }
        [JsonProperty]
        public List<Account> Accounts { get; set; }
        [JsonProperty]
        public string ThanksString { get; set; }
    }
}