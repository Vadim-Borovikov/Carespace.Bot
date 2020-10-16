using System;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models
{
    internal sealed class EventData
    {
        [JsonProperty]
        public int TemplateId { get; set; }
        [JsonProperty]
        public int MessageId { get; set; }
        [JsonProperty]
        public DateTime Start { get; set; }
        [JsonProperty]
        public DateTime End { get; set; }

        public EventData() { }

        public EventData(EventTemplate template) : this(template.Id, template.Start, template.End) { }

        public EventData(int templateId, DateTime start, DateTime end)
        {
            TemplateId = templateId;
            Start = start;
            End = end;
        }
    }
}
