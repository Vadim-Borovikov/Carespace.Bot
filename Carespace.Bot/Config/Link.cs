using System;

namespace Carespace.Bot.Config;

public sealed class Link
{
    public readonly string Name;
    internal readonly Uri Uri;
    internal readonly string? PhotoPath;

    public Link(string name, Uri uri, string? photoPath)
    {
        Name = name;
        Uri = uri;
        PhotoPath = photoPath;
    }
}
