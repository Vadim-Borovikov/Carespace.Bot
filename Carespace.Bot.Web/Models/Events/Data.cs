using System;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models.Events
{
    internal sealed class Data
    {
        [JsonProperty]
        public int MessageId { get; set; }
        [JsonProperty]
        public DateTime Start { get; set; }
        [JsonProperty]
        public DateTime End { get; set; }

        public Data() { }

        public Data(int messageId, DateTime start, DateTime end)
        {
            MessageId = messageId;
            Start = start;
            End = end;
        }
    }
}
