using System.Collections.Generic;
using System.Linq;
using Carespace.Bot.Events;
using GoogleSheetsManager;
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

    public Data? Convert()
    {
        if (ScheduleId is null)
        {
            return null;
        }

        Events ??= new Dictionary<int, EventData?>();
        Messages ??= new Dictionary<int, MessageData?>();

        if (Events.Values.Any(v => v is null) || Messages.Values.Any(m => m is null))
        {
            return null;
        }

        // ReSharper disable NullableWarningSuppressionIsUsed
        //   Just null-checked
        return new Data(ScheduleId.Value, Events!, Messages!);
    }
}
