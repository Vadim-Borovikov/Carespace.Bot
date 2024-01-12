using System;

namespace Carespace.Bot;

internal static class Week
{
    public static DateOnly GetNextThursday(DateOnly date)
    {
        int diff = (7 + DayOfWeek.Thursday - date.DayOfWeek) % 7;
        return date.AddDays(diff);
    }
}