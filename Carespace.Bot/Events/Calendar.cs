﻿using System.Collections.Generic;
using System.Text;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using NodaTime.Extensions;

namespace Carespace.Bot.Events
{
    public sealed class Calendar
    {
        public readonly byte[] IcsContent;
        public readonly string GoogleCalendarLink;

        internal Calendar(Template template)
        {
            var sb = new StringBuilder();
            sb.AppendLine(template.Description);
            sb.AppendLine();
            sb.Append($"Цена: {template.Price}");
            if (!string.IsNullOrWhiteSpace(template.Hosts))
            {
                sb.AppendLine();
                sb.Append($"Кто ведёт: {template.Hosts}");
            }
            string icsDescription = sb.ToString();
            sb.AppendLine();
            sb.AppendLine();
            sb.Append($"Принять участе: {template.Uri}");
            string googleDetails = sb.ToString();

            var rule = new RecurrencePattern(FrequencyType.Weekly);
            CalendarEvent icsEvent = AsIcsEvent(template, icsDescription, rule);
            IcsContent = GetIcsContent(icsEvent);

            GoogleCalendarLink = AsGoogleLink(template, googleDetails, rule.ToString());
        }

        private static byte[] GetIcsContent(CalendarEvent icsEvent)
        {
            var calendar = new Ical.Net.Calendar
            {
                Events = { icsEvent }
            };
            var serializer = new CalendarSerializer();
            string content = serializer.SerializeToString(calendar);
            return Encoding.UTF8.GetBytes(content);
        }

        private static CalendarEvent AsIcsEvent(Template template, string description, RecurrencePattern rule)
        {
            var e = new CalendarEvent
            {
                Start = new CalDateTime(template.Start.ToUniversalTime()),
                End = new CalDateTime(template.End.ToUniversalTime()),
                Summary = template.Name,
                Description = description,
                Url = template.Uri
            };
            if (template.IsWeekly)
            {
                e.RecurrenceRules = new List<RecurrencePattern> { rule };
            }
            return e;
        }

        private static string AsGoogleLink(Template template, string details, string rule)
        {
            template.Start.ToLocalDateTime();

            var queryString = new Dictionary<string, string>
            {
                ["action"] = "TEMPLATE" ,
                ["text"] = template.Name,
                ["dates"] = $"{template.Start.ToUniversalTime():yyyyMMddTHHmmssZ}/{template.End.ToUniversalTime():yyyyMMddTHHmmssZ}",
                ["details"] = details
            };
            if (template.IsWeekly)
            {
                queryString["recur"] = $"RRULE:{rule}";
            }

            return QueryHelpers.AddQueryString(GoogleUri, queryString);
        }

        private const string GoogleUri = "https://www.google.com/calendar/render";
    }
}
