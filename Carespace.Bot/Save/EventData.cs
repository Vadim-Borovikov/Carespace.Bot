using Carespace.Bot.Events;
using JetBrains.Annotations;

namespace Carespace.Bot.Save;

public sealed class EventData
{
    [UsedImplicitly]
    public int MessageId { get; set; }

    [UsedImplicitly]
    public int? NotificationId { get; set; }

    public EventData() { }

    internal EventData(Event e)
    {
        MessageId = e.MessageId;
        NotificationId = e.NotificationId;
    }
}