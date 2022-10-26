using System;
using JetBrains.Annotations;
using Telegram.Bot.Types;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.Bot.Save;

public sealed class MessageData
{
    public enum KeyboardType
    {
        None,
        Ics
    }

    [UsedImplicitly]
    public DateTime Date { get; set; }

    [UsedImplicitly]
    public string Text { get; set; } = null!;
    [UsedImplicitly]
    public KeyboardType Keyboard { get; set; }

    public MessageData() { }

    internal MessageData(Message message) => Date = message.Date.ToLocalTime();
}
