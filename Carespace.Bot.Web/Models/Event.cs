using System;

namespace Carespace.Bot.Web.Models
{
    internal sealed class Event
    {
        public readonly EventTemplate Template;
        public EventData Data;

        public Event(EventTemplate template)
        {
            Template = template;
            Data = new EventData(template);
        }

        public Event(EventTemplate template, DateTime weekStart)
        {
            Template = template;

            int weeks = (int) Math.Ceiling((weekStart - template.Start).TotalDays / 7);
            DateTime eventStart = template.Start.AddDays(7 * weeks);
            DateTime eventEnd = template.End.AddDays(7 * weeks);
            Data = new EventData(Template.Id, eventStart, eventEnd);
        }
    }
}
