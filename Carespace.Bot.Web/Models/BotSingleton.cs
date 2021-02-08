using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models
{
    public sealed class BotSingleton : IDisposable
    {
        internal readonly Bot Bot;

        public BotSingleton(IOptions<Config> options)
        {
            Config config = options.Value;

            if ((config.GoogleCredential == null) || (config.GoogleCredential.Count == 0))
            {
                config.GoogleCredential =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(config.GoogleCredentialJson);
            }
            Bot = new Bot(config);
        }

        public void Dispose() => Bot.Dispose();
    }
}