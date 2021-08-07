using System;
using System.Collections.Generic;
using AbstractBot;
using Newtonsoft.Json;

namespace Carespace.Bot.Config
{
    public class Config : ConfigGoogleSheets
    {
        [JsonProperty]
        public List<string> IntroductionLines { get; set; }

        [JsonProperty]
        public List<string> ScheduleLines { get; set; }

        [JsonProperty]
        public List<Link> Links { get; set; }

        [JsonProperty]
        public string Template { get; set; }

        [JsonProperty]
        public List<string> ExersisesLinks { get; set; }

        [JsonProperty]
        public Link FeedbackLink { get; set; }

        [JsonProperty]
        public string GoogleRange { get; set; }

        [JsonProperty]
        public string EventsChannelLogin { get; set; }

        [JsonProperty]
        public Uri EventsFormUri { get; set; }

        [JsonProperty]
        public DateTime EventsUpdateAt { get; set; }

        [JsonProperty]
        public string SavePath { get; set; }

        [JsonProperty]
        public string LogsChatId { get; set; }

        [JsonProperty]
        public string DiscussGroupLogin { get; set; }

        [JsonProperty]
        public int SellerId { get; set; }

        [JsonProperty]
        public int ProductId { get; set; }

        [JsonProperty]
        public DateTime SellsStart { get; set; }

        [JsonProperty]
        public string SellerSecret { get; set; }

        [JsonProperty]
        public string BookPromo { get; set; }

        internal string Introduction => string.Join('\n', IntroductionLines);
        internal string Schedule => string.Join('\n', ScheduleLines);
    }
}