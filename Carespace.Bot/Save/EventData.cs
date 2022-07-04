using Newtonsoft.Json;

namespace Carespace.Bot.Save;

internal sealed class EventData
{
    [JsonProperty]
    public int? MessageId { get; set; }

    [JsonProperty]
    public int? NotificationId { get; set; }

    public EventData() { }

    public EventData(int? messageId, int? notificationId)
    {
        MessageId = messageId;
        NotificationId = notificationId;
    }
}
