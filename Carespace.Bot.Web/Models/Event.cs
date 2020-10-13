using System;
using System.Collections.Generic;
using System.Linq;
using GoogleSheetsReader;

namespace Carespace.Bot.Web.Models
{
    public class Event : ILoadable
    {
        public int Id { get; private set; }
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }
        public string Name { get; private set; }
        public string Hosts { get; private set; }
        public string Description { get; private set; }
        public Uri Uri { get; private set; }
        public List<string> Tags { get; private set; }

        public void Load(IList<object> values, int index)
        {
            Id = index;

            Name = DataManager.ToString(values, 0);

            DateTime? start = DataManager.ToDateTime(values, 1);
            if (!start.HasValue)
            {
                throw new ArgumentNullException($"Empty start in \"{Name}\"");
            }
            Start = start.Value;

            DateTime? end = DataManager.ToDateTime(values, 2);
            if (!end.HasValue)
            {
                throw new ArgumentNullException($"Empty end in \"{Name}\"");
            }
            End = end.Value;

            Hosts = DataManager.ToString(values, 3);

            Description = DataManager.ToString(values, 4);

            Uri = DataManager.ToUri(values, 5);

            Tags = DataManager.ToString(values, 6)?.Split(';')?.ToList();
        }
    }
}
