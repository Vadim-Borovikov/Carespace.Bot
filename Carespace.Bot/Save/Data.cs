using System.Collections.Generic;
using System.Linq;
using Carespace.Bot.Events;
using GoogleSheetsManager;

namespace Carespace.Bot.Save;

internal sealed class Data : IConvertibleTo<JsonData>
{
    public int ScheduleId;

    public Dictionary<int, EventData> Events = new();

    public readonly Dictionary<int, MessageData> Messages = new();

    public Data() { }

    public Data(int scheduleId, Dictionary<int, EventData> events, Dictionary<int, MessageData> messages)
    {
        ScheduleId = scheduleId;
        Events = events;
        Messages = messages;
    }

    public JsonData Convert()
    {
        return new JsonData
        {
            ScheduleId = ScheduleId,
            Events = Events.ToDictionary(p => p.Key, p => (EventData?) p.Value),
            Messages = Messages.ToDictionary(p => p.Key, p => (MessageData?) p.Value)
        };
    }
}