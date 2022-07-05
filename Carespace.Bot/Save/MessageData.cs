using System;
using GoogleSheetsManager;
using Telegram.Bot.Types;

namespace Carespace.Bot.Save;

internal sealed class MessageData : IConvertibleTo<JsonMessageData>
{
    public enum KeyboardType
    {
        None,
        Ics
    }

    public readonly DateTime Date;

    public string Text;
    public KeyboardType Keyboard;

    public MessageData(Message message, string text, KeyboardType keyboard) :
        this(message.Date.ToLocalTime(), text, keyboard)
    {
    }

    public MessageData(DateTime date, string text, KeyboardType keyboard)
    {
        Date = date;
        Text = text;
        Keyboard = keyboard;
    }

    public JsonMessageData Convert() => new(Text, Keyboard, Date);
}
