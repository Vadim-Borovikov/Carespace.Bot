namespace Carespace.Bot.Web.Models.Events
{
    internal sealed class Event
    {
        public readonly Template Template;
        public readonly Data Data;

        public Event(Template template, Data data)
        {
            Template = template;
            Data = data;
        }
    }
}
