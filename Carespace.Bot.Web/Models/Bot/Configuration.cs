// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;
using System.Collections.Generic;

namespace Carespace.Bot.Web.Models.Bot
{
    public sealed class Configuration
    {
        public class Link
        {
            public string Name { get; set; }
            public string Url { get; set; }
            public string PhotoPath { get; set; }
        }

        public string Token { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public Dictionary<string, string> GoogleCredentials { get; set; }
        public string GoogleCredentialsJson { get; set; }

        public List<string> DocumentIds { get; set; }

        public string PdfFolderId { get; set; }

        public string PdfFolderPath { get; set; }

        public string Url => $"{Host}:{Port}/{Token}";

        public List<string> CheckListLines { get; set; }

        public string CheckList => string.Join('\n', CheckListLines);

        public List<int> AdminIds { get; set; }

        public List<Link> Links { get; set; }

        public string Template { get; set; }

        public List<string> ExersisesLinks { get; set; }

        public string GoogleSheetId { get; set; }

        public string GoogleRange { get; set; }

        public string EventsChannelLogin { get; set; }

        public Uri EventsFormUri { get; set; }

        public DateTime EventsUpdateAt { get; set; }

        public string SavePath { get; set; }

        public string LogsChatId { get; set; }

        public string DiscussGroupLogin { get; set; }
    }
}