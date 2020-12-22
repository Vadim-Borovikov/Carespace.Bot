using System.Collections.Generic;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models.Config
{
    public sealed class Payee
    {
        [JsonProperty]
        public string Name { get; set; }
        [JsonProperty]
        public string PhotoPath { get; set; }
        [JsonProperty]
        public List<Account> Accounts { get; set; }
    }
}