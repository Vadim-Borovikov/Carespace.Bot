using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models.Events
{
    internal sealed class Data
    {
        [JsonProperty]
        public int MessageId { get; set; }

        [JsonProperty]
        public int? NotificationId { get; set; }

        public Data() { }

        public Data(int messageId)
        {
            MessageId = messageId;
        }
    }
}
