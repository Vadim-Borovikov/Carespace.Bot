using GoogleSheetsManager;

namespace Carespace.Bot.Save;

internal sealed class EventData : IConvertibleTo<JsonEventData>
{
    public readonly int MessageId;

    public int? NotificationId;

    public EventData(int messageId, int? notificationId)
    {
        MessageId = messageId;
        NotificationId = notificationId;
    }

    public JsonEventData Convert() => new(MessageId, NotificationId);
}
