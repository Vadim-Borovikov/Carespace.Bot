using System;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models.Events
{
    internal sealed class Data
    {
        [JsonProperty]
        public int TemplateId { get; set; }
        [JsonProperty]
        public int MessageId { get; set; }
        [JsonProperty]
        public DateTime Start { get; set; }
        [JsonProperty]
        public DateTime End { get; set; }

        public Data() { }

        public Data(Template template) : this(template.Id, template.Start, template.End) { }

        public Data(int templateId, DateTime start, DateTime end)
        {
            TemplateId = templateId;
            Start = start;
            End = end;
        }
    }
}
