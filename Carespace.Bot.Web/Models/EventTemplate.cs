using System;
using System.Collections.Generic;
using GoogleSheetsManager;

namespace Carespace.Bot.Web.Models
{
    internal sealed class EventTemplate : ILoadable
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }
        public string Hosts { get; private set; }
        public string Description { get; private set; }
        public Uri Uri { get; private set; }
        public string Price { get; private set; }
        public bool IsWeekly { get; private set; }

        public void Load(IList<object> values)
        {
            Name = DataManager.ToString(values, 1);

            int? id = DataManager.ToInt(values, 0);
            if (!id.HasValue)
            {
                throw new ArgumentNullException($"Empty id in \"{Name}\"");
            }
            Id = id.Value;

            DateTime? start = DataManager.ToDateTime(values, 2);
            if (!start.HasValue)
            {
                throw new ArgumentNullException($"Empty start in \"{Name}\"");
            }
            Start = start.Value;

            DateTime? end = DataManager.ToDateTime(values, 3);
            if (!end.HasValue)
            {
                throw new ArgumentNullException($"Empty end in \"{Name}\"");
            }
            End = end.Value;

            Hosts = DataManager.ToString(values, 4);

            Description = DataManager.ToString(values, 5);

            Uri = DataManager.ToUri(values, 6);

            Price = DataManager.ToString(values, 7);

            IsWeekly = DataManager.To<bool?>(values, 8) ?? false;
        }
    }
}
