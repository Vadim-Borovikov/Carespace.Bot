using System;
using GryphonUtilities;
using Microsoft.Extensions.Options;

namespace Carespace.Bot.Web.Models;

public sealed class BotSingleton : IDisposable
{
    internal readonly Bot Bot;
    internal readonly Uri ErrorPageUri;

    public BotSingleton(IOptions<ConfigJson> options)
    {
        ConfigJson configJson = options.Value;
        Config.Config config = configJson.Convert();
        Bot = new Bot(config);

        ErrorPageUri = configJson.ErrorPageUri.GetValue(nameof(configJson.ErrorPageUri));
    }

    public void Dispose() => Bot.Dispose();
}