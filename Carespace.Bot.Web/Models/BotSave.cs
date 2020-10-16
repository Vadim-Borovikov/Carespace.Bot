using System.Collections.Generic;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models
{
    internal sealed class BotSave
    {
        [JsonProperty]
        public List<EventData> Events { get; set; } = new List<EventData>();
        [JsonProperty]
        public Dictionary<int, string> Texts { get; set; } = new Dictionary<int, string>();
    }
}