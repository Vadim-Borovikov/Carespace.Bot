using System;
using GoogleSheetsManager.Extensions;

namespace Carespace.FinanceHelper;

internal static class Hyperlink
{
    public static string? From(string urlFormat, object? parameter)
    {
        string? caption = parameter?.ToString();
        Uri? uri = string.IsNullOrWhiteSpace(caption) ? null : new Uri(string.Format(urlFormat, caption));
        return uri?.ToHyperlink(caption);
    }
}