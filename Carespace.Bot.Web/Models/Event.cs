using System;
using System.Collections.Generic;
using System.Linq;
using GoogleSheetsManager;

namespace Carespace.Bot.Web.Models
{
    internal class Event : ILoadable, ISavable
    {
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }
        public string Name { get; private set; }
        public string Hosts { get; private set; }
        public string Description { get; private set; }
        public Uri Uri { get; private set; }
        public List<string> Tags { get; private set; }
        public bool IsWeekly { get; private set; }
        public int? DescriptionId { get; set; }

        public void Load(IList<object> values, int index)
        {
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

            string tags = DataManager.ToString(values, 6);
            Tags = !string.IsNullOrWhiteSpace(tags) ? tags.Split(';')?.ToList() : new List<string>();

            IsWeekly = DataManager.To<bool?>(values, 7) ?? false;

            DescriptionId = DataManager.ToInt(values, 8);
        }

        public IList<object> Save()
        {
            var result = new List<object>
            {
                Name,
                $"{Start:dd.MMMM.yyyy HH:mm}",
                $"{End:dd.MMMM.yyyy HH:mm}",
                $"{Hosts}",
                $"{Description}",
                $"{Uri}",
                Tags == null ? "" : string.Join(';', Tags),
                $"{IsWeekly}",
                $"{DescriptionId}"
            };

            return result;
        }

        public void PlaceOnWeek(DateTime start)
        {
            int weeks = (int) Math.Ceiling((start - Start).TotalDays / 7);
            Start = Start.AddDays(7 * weeks);
            End = End.AddDays(7 * weeks);
        }
    }
}
