using System.Collections.Generic;
using Carespace.Bot.Web.Models.Events;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models
{
    internal sealed class BotSave
    {
        [JsonProperty]
        public Dictionary<int, Data> Events { get; set; } = new Dictionary<int, Data>();
        [JsonProperty]
        public Dictionary<int, string> Texts { get; set; } = new Dictionary<int, string>();
    }
}