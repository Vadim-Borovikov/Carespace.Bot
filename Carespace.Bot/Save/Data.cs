using System.Collections.Generic;
using JetBrains.Annotations;

namespace Carespace.Bot.Save;

public sealed class Data
{
    [UsedImplicitly]
    public int? ScheduleId { get; set; }

    [UsedImplicitly]
    public Dictionary<int, EventData> Events { get; set; } = new();

    [UsedImplicitly]
    public Dictionary<long, byte> Strikes { get; set; } = new();

    [UsedImplicitly]
    public Dictionary<int, MessageData> Messages { get; set; } = new();
}