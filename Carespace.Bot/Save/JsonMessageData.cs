using System;
using GoogleSheetsManager;
using GryphonUtilities;
using Newtonsoft.Json;

namespace Carespace.Bot.Save;

internal sealed class JsonMessageData : IConvertibleTo<MessageData>
{
    [JsonProperty]
    public string? Text { get; set; }

    [JsonProperty]
    public MessageData.KeyboardType? Keyboard { get; set; }

    [JsonProperty]
    public DateTime? Date { get; set; }

    public JsonMessageData() { }

    public JsonMessageData(string? text, MessageData.KeyboardType? keyboard, DateTime? date)
    {
        Text = text;
        Keyboard = keyboard;
        Date = date;
    }

    public MessageData Convert() => new(Date.GetValue(), Text.GetValue(), Keyboard.GetValue());
}
