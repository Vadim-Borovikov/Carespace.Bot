﻿using System;
using System.Collections.Generic;
using GoogleSheetsManager;
using ILoadable = GoogleSheetsManager.ILoadable;

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
            int? id = DataManager.ToInt(values, 0);
            IsApproved = id.HasValue;
            if (!id.HasValue)
            {
                return;
            }
            Id = id.Value;

            Name = DataManager.ToString(values, 1);

            Description = DataManager.ToString(values, 2);

            DateTime startDate = DataManager.ToDateTime(values, 3)
                ?? throw new ArgumentNullException($"Empty start date in \"{Name}\"");

            _skip = DataManager.ToDateTime(values, 4);

            DateTime startTime = DataManager.ToDateTime(values, 5)
                ?? throw new ArgumentNullException($"Empty start time in \"{Name}\"");

            Start = startDate.Date + startTime.TimeOfDay;

            TimeSpan duration = DataManager.ToTimeSpan(values, 6)
                ?? throw new ArgumentNullException($"Empty duration in \"{Name}\"");

            End = Start + duration;

            Hosts = DataManager.ToString(values, 7);

            Price = DataManager.ToString(values, 8);

            string type = DataManager.ToString(values, 9);
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

            Uri = DataManager.ToUri(values, 10) ?? throw new ArgumentNullException($"Empty uri in \"{Name}\"");
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
