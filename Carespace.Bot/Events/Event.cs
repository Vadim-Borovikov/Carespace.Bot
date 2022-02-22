using System;
using AbstractBot;

namespace Carespace.Bot.Events;

internal sealed class Event : IDisposable
{
    public readonly Template Template;
    public readonly EventData Data;
    public readonly Timer Timer;

    public Event(Template template, EventData data, TimeManager timeManager)
    {
        Template = template;
        Data = data;
        Timer = new Timer(timeManager);
    }

    public void Dispose() => DisposeTimer();

    public void DisposeTimer()
    {
        Timer.Stop();
        Timer.Dispose();
    }
}
