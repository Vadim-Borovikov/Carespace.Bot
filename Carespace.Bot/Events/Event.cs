using System;

namespace Carespace.Bot.Events;

internal sealed class Event : IDisposable
{
    public readonly Template Template;
    public readonly int MessageId;
    public int? NotificationId;
    public Timer? Timer { get; private set; }

    public Event(Template template, int messageId, int? notificationId = null)
    {
        Template = template;
        MessageId = messageId;
        NotificationId = notificationId;
        Timer = new Timer();
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
