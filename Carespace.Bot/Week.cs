using System;
using GryphonUtilities;

namespace Carespace.Bot;

internal static class Week
{
    public static DateOnly GetMonday(TimeManager timeManager)
    {
        DateOnly today = timeManager.Now().DateOnly;
        int diff = (7 + today.DayOfWeek - DayOfWeek.Monday) % 7;
        return today.AddDays(-diff);
    }
}