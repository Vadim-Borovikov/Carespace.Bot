using System.Collections.Generic;
using System.Linq;
using GoogleSheetsManager;
using GryphonUtilities;
using Newtonsoft.Json;

namespace Carespace.Bot.Save;

internal sealed class JsonData : IConvertibleTo<Data>
{
    [JsonProperty]
    public int? ScheduleId { get; set; }

    [JsonProperty]
    public Dictionary<int, EventData?>? Events { get; set; }

    [JsonProperty]
    public Dictionary<int, MessageData?>? Messages { get; set; }

    public Data Convert()
    {
        Dictionary<int, EventData> events = Events.GetValue().ToDictionary(p => p.Key, p => p.Value.GetValue());
        Dictionary<int, MessageData> messages = Messages.GetValue().ToDictionary(p => p.Key, p => p.Value.GetValue());
        return new Data(ScheduleId, events, messages);
    }
}
