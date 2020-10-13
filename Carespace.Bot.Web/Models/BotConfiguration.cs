// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable ClassNeverInstantiated.Global

using System;
using System.Collections.Generic;

namespace Carespace.Bot.Web.Models
{
    public class BotConfiguration
    {
        public class Link
        {
            public string Name { get; set; }
            public string Url { get; set; }
            public string PhotoPath { get; set; }
        }

        public class Payee
        {
            public class Account
            {
                public string BankId { get; set; }
                public string CardNumber { get; set; }
            }

            public string Name { get; set; }
            public string PhotoPath { get; set; }
            public List<Account> Accounts { get; set; }
        }

        public string Token { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public int PingPeriodSeconds { get; set; }

        public TimeSpan PingPeriod => TimeSpan.FromSeconds(PingPeriodSeconds);

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

        public Link FeedbackLink { get; set; }

        public List<Payee> Payees { get; set; }

        public Dictionary<string, Link> Banks { get; set; }

        public string GoogleSheetId { get; set; }

        public string GoogleEventsRangeAll { get; set; }
        public string GoogleEventsRangePin { get; set; }
        public string GoogleEventsRangeWeek { get; set; }

        public string EventsChannelLogin { get; set; }
    }
}