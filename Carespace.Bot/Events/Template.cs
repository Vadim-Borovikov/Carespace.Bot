using System;
using System.Collections.Generic;
using GoogleSheetsManager;
using GryphonUtilities;

namespace Carespace.Bot.Events;

internal sealed class Template
{
    public readonly int Id;
    public readonly string Name;
    public readonly string Description;
    public readonly string? Hosts;
    public readonly string Price;
    public readonly bool IsWeekly;
    public readonly Uri Uri;

    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }

    public bool Active => !IsWeekly || (_skip != Start.Date);

    private Template(int id, string name, string description, string? hosts, string price, bool isWeekly, Uri uri,
        DateTime start, DateTime end, DateTime? skip)
    {
        Id = id;
        Name = name;
        Description = description;
        Start = start;
        End = end;
        Hosts = hosts;
        Price = price;
        IsWeekly = isWeekly;
        Uri = uri;
        _skip = skip;
    }

    public static Template? Load(IDictionary<string, object?> valueSet)
    {
        int? id = valueSet[IdTitle]?.ToInt();
        if (id is null)
        {
            return null;
        }
        int idValue = id.Value;

        string? name = valueSet[NameTitle]?.ToString();
        string nameValue = name.GetValue("Empty template name");

        string? description = valueSet[DescriptionTitle]?.ToString();
        string descriptionValue = description.GetValue("Empty template description");

        string? hosts = valueSet[HostsTitle]?.ToString();

        string? price = valueSet[PriceTitle]?.ToString();
        string priceValue = price.GetValue($"Empty price in \"{nameValue}\"");

        string? type = valueSet[TypeTitle]?.ToString();
        bool isWeekly = type switch
        {
            "Еженедельное" => true,
            "Однократное"  => false,
            _              => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        Uri uri = valueSet[UriTitle].ToUri().GetValue($"Empty uri in \"{nameValue}\"");

        DateTime startDate = valueSet[StartDateTitle].ToDateTime().GetValue($"Empty start date in \"{nameValue}\"");
        DateTime startTime = valueSet[StartTimeTitle].ToDateTime().GetValue($"Empty start time in \"{nameValue}\"");
        DateTime start = startDate.Date + startTime.TimeOfDay;

        TimeSpan duration = valueSet[DurationTitle].ToTimeSpan().GetValue($"Empty duration in \"{nameValue}\"");
        DateTime end = start + duration;

        DateTime? skip = valueSet[SkipTitle].ToDateTime();

        return new Template(idValue, nameValue, descriptionValue, hosts, priceValue, isWeekly, uri, start, end, skip);
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

    private readonly DateTime? _skip;
}