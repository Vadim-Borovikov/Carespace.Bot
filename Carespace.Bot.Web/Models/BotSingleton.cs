using System;

namespace Carespace.Bot.Web.Models;

public sealed class BotSingleton : IDisposable
{
    internal readonly Bot Bot;
    internal readonly Uri ErrorPageUri;

    public BotSingleton(Config config)
    {
        Bot = new Bot(config);
        ErrorPageUri = config.ErrorPageUri;
    }

    public void Dispose() => Bot.Dispose();
}