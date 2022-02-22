using System;
using System.Collections.Generic;
using System.Text;
using AbstractBot;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.WebUtilities;

namespace Carespace.Bot.Events;

public sealed class Calendar
{
    public readonly byte[] IcsContent;
    public readonly string GoogleCalendarLink;

    public bool IsOver => DateTime.UtcNow > _endTime;

    internal Calendar(Template template, TimeManager timeManager)
    {
        _timeManager = timeManager;
        _endTime = _timeManager.ToUtc(template.End);

        StringBuilder sb = new();
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

        RecurrencePattern rule = new(FrequencyType.Weekly);
        CalendarEvent icsEvent = AsIcsEvent(template, icsDescription, rule);
        IcsContent = GetIcsContent(icsEvent);

        GoogleCalendarLink = AsGoogleLink(template, googleDetails, rule.ToString());
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

    private CalendarEvent AsIcsEvent(Template template, string description, RecurrencePattern rule)
    {
        CalendarEvent e = new()
        {
            Start = new CalDateTime(_timeManager.ToUtc(template.Start)),
            End = new CalDateTime(_endTime),
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

    private string AsGoogleLink(Template template, string details, string rule)
    {
        Dictionary<string, string> queryString = new()
        {
            ["action"] = "TEMPLATE" ,
            ["text"] = template.Name,
            ["dates"] = $"{_timeManager.ToUtc(template.Start):yyyyMMddTHHmmssZ}/{_timeManager.ToUtc(template.End):yyyyMMddTHHmmssZ}",
            ["details"] = details
        };
        if (template.IsWeekly)
        {
            queryString["recur"] = $"RRULE:{rule}";
        }

        return QueryHelpers.AddQueryString(GoogleUri, queryString);
    }

    private const string GoogleUri = "https://www.google.com/calendar/render";
    private readonly TimeManager _timeManager;
    private readonly DateTime _endTime;
}
