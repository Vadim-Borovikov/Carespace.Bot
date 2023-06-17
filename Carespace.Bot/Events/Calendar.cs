using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using GryphonUtilities;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace Carespace.Bot.Events;

public sealed class Calendar
{
    public readonly byte[] IcsContent;
    public readonly string GoogleCalendarLink;

    public bool IsOver => DateTimeFull.CreateUtcNow() > _template.GetEnd(_timeManager);

    internal Calendar(Template template, TimeManager timeManager)
    {
        _template = template;
        _timeManager = timeManager;

        StringBuilder sb = new();
        sb.AppendLine(_template.Description);
        sb.AppendLine();
        sb.Append($"Цена: {_template.Price}");
        if (!string.IsNullOrWhiteSpace(_template.Hosts))
        {
            sb.AppendLine();
            sb.Append($"Кто ведёт: {_template.Hosts}");
        }
        string icsDescription = sb.ToString();
        sb.AppendLine();
        sb.AppendLine();
        sb.Append($"Принять участе: {_template.Uri}");
        string googleDetails = sb.ToString();

        RecurrencePattern rule = new(FrequencyType.Weekly);
        CalendarEvent icsEvent = AsIcsEvent(icsDescription, rule);
        IcsContent = GetIcsContent(icsEvent);

        GoogleCalendarLink = AsGoogleLink(googleDetails, rule.ToString());
    }

    private static byte[] GetIcsContent(CalendarEvent icsEvent)
    {
        Ical.Net.Calendar calendar = new()
        {
            Events = { icsEvent }
        };
        CalendarSerializer serializer = new();
        string content = serializer.SerializeToString(calendar);
        return Encoding.UTF8.GetBytes(content);
    }

    private CalendarEvent AsIcsEvent(string description, RecurrencePattern rule)
    {
        CalendarEvent e = new()
        {
            Start = new CalDateTime(_template.GetStart(_timeManager).UtcDateTime),
            End = new CalDateTime(_template.GetEnd(_timeManager).UtcDateTime),
            Summary = _template.Name,
            Description = description,
            Url = _template.Uri
        };
        if (_template.IsWeekly)
        {
            e.RecurrenceRules = new List<RecurrencePattern> { rule };
        }
        return e;
    }

    private string AsGoogleLink(string details, string rule)
    {
        NameValueCollection query = HttpUtility.ParseQueryString("");
        query["action"] = "TEMPLATE";
        query["text"] = _template.Name;
        query["dates"] =
            $"{_template.GetStart(_timeManager).UtcDateTime:yyyyMMddTHHmmssZ}/{_template.GetEnd(_timeManager).UtcDateTime:yyyyMMddTHHmmssZ}";
        query["details"] = details;
        if (_template.IsWeekly)
        {
            query["recur"] = $"RRULE:{rule}";
        }

        UriBuilder uriBuilder = new(GoogleUri)
        {
            Query = query.ToString()
        };
        return uriBuilder.ToString();
    }

    private const string GoogleUri = "https://www.google.com/calendar/render";
    private readonly Template _template;
    private readonly TimeManager _timeManager;
}
