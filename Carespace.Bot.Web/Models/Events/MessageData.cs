using System;
using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Models.Events
{
    internal sealed class MessageData
    {
        [JsonProperty]
        public string Text { get; set; }

        [JsonProperty]
        public DateTime Date { get; set; }

        public MessageData() { }

        public MessageData(Message message, string text)
        {
            Text = text;
            Date = message.Date.ToLocalTime();
        }
    }
}
