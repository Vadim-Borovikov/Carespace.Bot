using System.Collections.Generic;
using System.Linq;
using GryphonUtilities;
using Newtonsoft.Json;

namespace Carespace.Bot.Save;

internal sealed class JsonData
{
    [JsonProperty]
    public int? ScheduleId { get; set; }

    [JsonProperty]
    public Dictionary<int, JsonEventData?>? Events { get; set; }

    [JsonProperty]
    public Dictionary<int, JsonMessageData?>? Messages { get; set; }

    public static Data? Convert(JsonData? jsonData)
    {
        if (jsonData is null)
        {
            return null;
        }

        Dictionary<int, EventData> events =
            jsonData.Events.GetValue().ToDictionary(p => p.Key, p => p.Value.GetValue().Convert());
        Dictionary<int, MessageData> messages =
            jsonData.Messages.GetValue().ToDictionary(p => p.Key, p => p.Value.GetValue().Convert());
        return new Data(jsonData.ScheduleId, events, messages);
    }
}
