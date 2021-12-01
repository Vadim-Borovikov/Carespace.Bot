using System;
using System.Collections.Generic;
using GoogleSheetsManager;

namespace Carespace.Bot.Events
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

        public void Load(IDictionary<string, object> valueSet)
        {
            int? id = valueSet[IdTitle]?.ToInt();
            IsApproved = id.HasValue;
            if (!id.HasValue)
            {
                return;
            }
            Id = id.Value;

            Name = valueSet[NameTitle]?.ToString();

            Description = valueSet[DescriptionTitle]?.ToString();

            DateTime startDate =
                valueSet[StartDateTitle]?.ToDateTime() ?? throw new ArgumentNullException($"Empty start date in \"{Name}\"");

            _skip = valueSet[SkipTitle]?.ToDateTime();

            DateTime startTime =
                valueSet[StartTimeTitle]?.ToDateTime() ?? throw new ArgumentNullException($"Empty start time in \"{Name}\"");

            Start = startDate.Date + startTime.TimeOfDay;

            TimeSpan duration =
                valueSet[DurationTitle]?.ToTimeSpan() ?? throw new ArgumentNullException($"Empty duration in \"{Name}\"");

            End = Start + duration;

            Hosts = valueSet[HostsTitle]?.ToString();

            Price = valueSet[PriceTitle]?.ToString();

            string type = valueSet[TypeTitle]?.ToString();
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

            Uri = valueSet[UriTitle]?.ToUri() ?? throw new ArgumentNullException($"Empty uri in \"{Name}\"");
        }

        public void MoveToWeek(DateTime weekStart)
        {
            int weeks = (int) Math.Ceiling((weekStart - Start).TotalDays / 7);
            Start = Start.AddDays(7 * weeks);
            End = End.AddDays(7 * weeks);
        }

        private const string IdTitle = "Id";
        private const string NameTitle = "Как называется ваше мероприятие?";
        private const string DescriptionTitle = "Расскажите о нём побольше";
        private const string StartDateTitle = "Дата проведения";
        private const string SkipTitle = "Пропуск";
        private const string StartTimeTitle = "Время начала";
        private const string DurationTitle = "Продолжительность";
        private const string HostsTitle = "Кто будет вести?";
        private const string PriceTitle = "Цена";
        private const string TypeTitle = "Тип события";
        private const string UriTitle = "Ссылка для регистрации или участия";

        private DateTime? _skip;
    }
}
