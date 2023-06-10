using System;
using GryphonUtilities;
using JetBrains.Annotations;
using Telegram.Bot.Types;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.Bot.Save;

public sealed class MessageData
{
    [Flags]
    public enum KeyboardType
    {
        Participate = 1 << 0,
        Ics = 1 << 1,
        Discuss = 1 << 2
    }

    [UsedImplicitly]
    public DateOnly Date { get; set; }

    [UsedImplicitly]
    public string Text { get; set; } = null!;
    [UsedImplicitly]
    public KeyboardType Keyboard { get; set; }
    [UsedImplicitly]
    public string? ButtonUri { get; set; }

    public MessageData() { }

    internal MessageData(Message message, TimeManager timeManager)
    {
        DateTimeFull utc = timeManager.GetDateTimeFull(message.Date);
        Date = timeManager.Convert(utc).DateOnly;
    }
}