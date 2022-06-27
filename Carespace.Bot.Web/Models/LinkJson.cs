using System;
using Carespace.Bot.Config;
using GoogleSheetsManager;
using GryphonUtilities;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models;

public sealed class LinkJson : IConvertibleTo<Link>
{
    [JsonProperty]
    public string? Name { get; set; }

    [JsonProperty]
    public Uri? Uri { get; set; }

    [JsonProperty]
    public string? PhotoPath { get; set; }

    public Link Convert()
    {
        string name = Name.GetValue(nameof(Name));
        Uri uri = Uri.GetValue(nameof(Uri));
        return new Link(name, uri, PhotoPath);
    }
}