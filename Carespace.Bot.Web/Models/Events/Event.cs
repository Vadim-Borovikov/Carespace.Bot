using System;

namespace Carespace.Bot.Web.Models.Events
{
    internal sealed class Event : IDisposable
    {
        public readonly Template Template;
        public readonly EventData Data;
        public readonly Timer Timer;

        public Event(Template template, EventData data)
        {
            Template = template;
            Data = data;
            Timer = new Timer();
        }

        public void Dispose() => DisposeTimer();

        public void DisposeTimer()
        {
            Timer?.Stop();
            Timer?.Dispose();
        }
    }
}
