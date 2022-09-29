using System.Collections.Generic;
using System.Linq;

namespace Carespace.Bot.Save;

internal sealed class Data
{
    public int? ScheduleId;

    public Dictionary<int, EventData> Events = new();

    public readonly Dictionary<int, MessageData> Messages = new();

    public Data() { }

    public Data(int? scheduleId, Dictionary<int, EventData> events, Dictionary<int, MessageData> messages)
    {
        ScheduleId = scheduleId;
        Events = events;
        Messages = messages;
    }

    public static JsonData? Convert(Data? data)
    {
        if (data is null)
        {
            return null;
        }

        return new JsonData
        {
            ScheduleId = data.ScheduleId,
            Events = data.Events.ToDictionary(p => p.Key, p => (JsonEventData?) p.Value.Convert()),
            Messages = data.Messages.ToDictionary(p => p.Key, p => (JsonMessageData?) p.Value.Convert())
        };
    }
}
