using System;
using GoogleSheetsManager.Extensions;
using GryphonUtilities;

namespace Carespace.Bot;

internal static class ObjectExtensions
{
    public static Uri? ToUri(this object? o)
    {
        if (o is Uri uri)
        {
            return uri;
        }
        string? uriString = o?.ToString();
        return string.IsNullOrWhiteSpace(uriString) ? null : new Uri(uriString);
    }

    public static DateOnly? ToDateOnly(this object? o, TimeManager timeManager)
    {
        if (o is DateOnly d)
        {
            return d;
        }

        DateTimeFull? dtf = o.ToDateTimeFull(timeManager);
        return dtf?.DateOnly;
    }

    public static TimeOnly? ToTimeOnly(this object? o, TimeManager timeManager)
    {
        if (o is TimeOnly t)
        {
            return t;
        }

        DateTimeFull? dtf = o.ToDateTimeFull(timeManager);
        return dtf?.TimeOnly;
    }

    public static TimeSpan? ToTimeSpan(this object? o, TimeManager timeManager)
    {
        if (o is TimeSpan t)
        {
            return t;
        }

        DateTimeFull? dtf = o.ToDateTimeFull(timeManager);
        return dtf?.DateTimeOffset.TimeOfDay;
    }

}