using System.Collections.Generic;
using Carespace.Bot.Web.Models.Events;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models
{
    internal sealed class BotSave
    {
        [JsonProperty]
        public Dictionary<int, EventData> Events { get; set; } = new Dictionary<int, EventData>();
        [JsonProperty]
        public Dictionary<int, MessageData> Messages { get; set; } = new Dictionary<int, MessageData>();
    }
}