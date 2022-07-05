using GoogleSheetsManager;
using GryphonUtilities;
using Newtonsoft.Json;

namespace Carespace.Bot.Save;

internal sealed class JsonEventData : IConvertibleTo<EventData>
{
    [JsonProperty]
    public int? MessageId { get; set; }

    [JsonProperty]
    public int? NotificationId { get; set; }

    public JsonEventData() { }

    public JsonEventData(int? messageId, int? notificationId)
    {
        MessageId = messageId;
        NotificationId = notificationId;
    }

    public EventData Convert() => new(MessageId.GetValue(), NotificationId);
}
