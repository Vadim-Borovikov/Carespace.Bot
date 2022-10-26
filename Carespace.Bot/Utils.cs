using System;
using System.Globalization;
using AbstractBot;
using File = System.IO.File;

namespace Carespace.Bot;

internal static class Utils
{
    public static void LogTimers(string text) => File.WriteAllText(TimersLogPath, $"{text}");

    public static DateTime GetMonday(TimeManager timeManager)
    {
        DateTime today = timeManager.Now().Date;
        int diff = (7 + today.DayOfWeek - DayOfWeek.Monday) % 7;
        return today.AddDays(-diff);
    }

    public static string ShowDate(DateTime date)
    {
        string day = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(date.ToString("dddd"));
        return $"{day}, {date:d MMMM}";
    }

    public static Uri? ToUri(object? o)
    {
        if (o is Uri uri)
        {
            return uri;
        }
        string? uriString = o?.ToString();
        return string.IsNullOrWhiteSpace(uriString) ? null : new Uri(uriString);
    }

    public const string CalendarUriFormat = "{0}/calendar/{1}";

    private const string TimersLogPath = "timers.txt";
}