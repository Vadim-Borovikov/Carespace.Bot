using System;
using GryphonUtilities;
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
    public DateOnly Date { get; set; }

    [UsedImplicitly]
    public string Text { get; set; } = null!;
    [UsedImplicitly]
    public KeyboardType Keyboard { get; set; }

    public MessageData() { }

    internal MessageData(Message message, TimeManager timeManager)
    {
        Date = timeManager.GetDateTimeFull(message.Date).DateOnly;
    }
}