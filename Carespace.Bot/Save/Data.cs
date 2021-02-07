using System.Collections.Generic;
using Carespace.Bot.Events;
using Newtonsoft.Json;

namespace Carespace.Bot.Save
{
    internal sealed class Data
    {
        [JsonProperty]
        public int ScheduleId { get; set; }
        [JsonProperty]
        public Dictionary<int, EventData> Events { get; set; } = new Dictionary<int, EventData>();
        [JsonProperty]
        public Dictionary<int, MessageData> Messages { get; set; } = new Dictionary<int, MessageData>();
    }
}