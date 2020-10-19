using System.Timers;

namespace Carespace.Bot.Web.Models.Events
{
    internal sealed class Event
    {
        public readonly Template Template;
        public readonly Data Data;
        public Timer Timer;

        public Event(Template template, Data data)
        {
            Template = template;
            Data = data;
            Timer = new Timer();
        }
    }
}
