﻿using System;
using GryphonUtilities;

namespace Carespace.Bot.Events;

internal sealed class Event : IDisposable
{
    public readonly Template Template;
    public readonly int MessageId;
    public int? NotificationId;
    public Timer? Timer { get; private set; }

    public Event(Template template, int messageId, Logger logger, int? notificationId = null)
    {
        Template = template;
        MessageId = messageId;
        NotificationId = notificationId;
        Timer = new Timer(logger);
    }

    public void Dispose() => DisposeTimer();

    public void DisposeTimer()
    {
        if (Timer is null)
        {
            return;
        }
        Timer.Dispose();
        Timer = null;
    }
}
