using System.Collections.Generic;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models.Config
{
    public sealed class Config
    {
        [JsonProperty]
        public string Token { get; set; }

        [JsonProperty]
        public string Host { get; set; }

        [JsonProperty]
        public int Port { get; set; }

        [JsonProperty]
        public List<string> IntroductionLines { get; set; }

        [JsonProperty]
        public List<Link> Links { get; set; }

        [JsonProperty]
        public string Template { get; set; }

        [JsonProperty]
        public List<string> ExersisesLinks { get; set; }

        [JsonProperty]
        public Link FeedbackLink { get; set; }

        [JsonProperty]
        public List<Payee> Payees { get; set; }

        [JsonProperty]
        public Dictionary<string, Link> Banks { get; set; }

        internal string Url => $"{Host}:{Port}/{Token}";

        internal string Introduction => string.Join('\n', IntroductionLines);
    }
}