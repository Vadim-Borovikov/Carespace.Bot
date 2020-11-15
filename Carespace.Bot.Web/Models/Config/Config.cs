using System;
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
        public Dictionary<string, string> GoogleCredentials { get; set; }

        [JsonProperty]
        public string GoogleCredentialsJson { get; set; }

        [JsonProperty]
        public List<string> DocumentIds { get; set; }

        [JsonProperty]
        public string PdfFolderId { get; set; }

        [JsonProperty]
        public string PdfFolderPath { get; set; }

        [JsonProperty]
        public List<string> CheckListLines { get; set; }

        [JsonProperty]
        public List<int> AdminIds { get; set; }

        [JsonProperty]
        public List<Link> Links { get; set; }

        [JsonProperty]
        public string Template { get; set; }

        [JsonProperty]
        public List<string> ExersisesLinks { get; set; }

        [JsonProperty]
        public string GoogleSheetId { get; set; }

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
        public string SystemTimeZoneId { get; set; }

        internal string Url => $"{Host}:{Port}/{Token}";

        internal string CheckList => string.Join('\n', CheckListLines);
    }
}