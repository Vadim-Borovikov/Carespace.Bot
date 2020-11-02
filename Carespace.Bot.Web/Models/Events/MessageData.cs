using System;
using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace Carespace.Bot.Web.Models.Events
{
    internal sealed class MessageData
    {
        public enum KeyboardType
        {
            None,
            Ics,
            Discuss,
            Full
        }

        [JsonProperty]
        public string Text { get; set; }

        [JsonProperty]
        public KeyboardType Keyboard { get; set; }

        [JsonProperty]
        public DateTime Date { get; set; }

        public MessageData() { }

        public MessageData(Message message, string text, KeyboardType keyboard)
        {
            Text = text;
            Date = message.Date.ToLocalTime();
            Keyboard = keyboard;
        }
    }
}
