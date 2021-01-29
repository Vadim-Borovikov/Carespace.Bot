using System;
using System.Collections.Generic;
using GoogleSheetsManager;

namespace Carespace.Bot.Web.Models.Events
{
    internal sealed class Template : ILoadable
    {
        public bool IsApproved { get; private set; }
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }
        public string Hosts { get; private set; }
        public string Price { get; private set; }
        public bool IsWeekly { get; private set; }
        public Uri Uri { get; private set; }

        public bool Active => !IsWeekly || (_skip != Start.Date);

        public void Load(IList<object> values)
        {
            int? id = values.ToInt(0);
            IsApproved = id.HasValue;
            if (!id.HasValue)
            {
                return;
            }
            Id = id.Value;

            Name = values.ToString(1);

            Description = values.ToString(2);

            DateTime startDate =
                values.ToDateTime(3) ?? throw new ArgumentNullException($"Empty start date in \"{Name}\"");

            _skip = values.ToDateTime(4);

            DateTime startTime =
                values.ToDateTime(5) ?? throw new ArgumentNullException($"Empty start time in \"{Name}\"");

            Start = startDate.Date + startTime.TimeOfDay;

            TimeSpan duration =
                values.ToTimeSpan(6) ?? throw new ArgumentNullException($"Empty duration in \"{Name}\"");

            End = Start + duration;

            Hosts = values.ToString(7);

            Price = values.ToString(8);

            string type = values.ToString(9);
            switch (type)
            {
                case "Еженедельное":
                    IsWeekly = true;
                    break;
                case "Однократное":
                    IsWeekly = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown type \"{type}\" in \"{Name}\"");
            }

            Uri = values.ToUri(10) ?? throw new ArgumentNullException($"Empty uri in \"{Name}\"");
        }

        public void MoveToWeek(DateTime weekStart)
        {
            int weeks = (int) Math.Ceiling((weekStart - Start).TotalDays / 7);
            Start = Start.AddDays(7 * weeks);
            End = End.AddDays(7 * weeks);
        }

        private DateTime? _skip;
    }
}
