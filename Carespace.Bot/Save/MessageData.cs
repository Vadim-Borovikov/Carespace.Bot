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
        Participate,
        Discuss,
        Full
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