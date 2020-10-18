using System;

namespace Carespace.Bot.Web.Models.Events
{
    internal sealed class Event
    {
        public readonly Template Template;
        public Data Data;

        public Event(Template template)
        {
            Template = template;
            Data = new Data(template);
        }

        public Event(Template template, DateTime weekStart)
        {
            Template = template;

            int weeks = (int) Math.Ceiling((weekStart - template.Start).TotalDays / 7);
            DateTime eventStart = template.Start.AddDays(7 * weeks);
            DateTime eventEnd = template.End.AddDays(7 * weeks);
            Data = new Data(Template.Id, eventStart, eventEnd);
        }
    }
}
