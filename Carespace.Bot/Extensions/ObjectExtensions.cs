using System;

namespace Carespace.Bot.Extensions;

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
}