using System;
using System.Globalization;
using GryphonUtilities;
using File = System.IO.File;

namespace Carespace.Bot;

internal static class Utils
{
    public static void LogTimers(string text) => File.WriteAllText(TimersLogPath, $"{text}");

    public static DateOnly GetMonday(TimeManager timeManager)
    {
        DateOnly today = timeManager.Now().DateOnly;
        int diff = (7 + today.DayOfWeek - DayOfWeek.Monday) % 7;
        return today.AddDays(-diff);
    }

    public static string ShowDate(DateOnly date)
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