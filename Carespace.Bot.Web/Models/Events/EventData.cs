using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models.Events
{
    internal sealed class EventData
    {
        [JsonProperty]
        public int MessageId { get; set; }

        [JsonProperty]
        public int? NotificationId { get; set; }

        public EventData() { }

        public EventData(int messageId)
        {
            MessageId = messageId;
        }
    }
}
