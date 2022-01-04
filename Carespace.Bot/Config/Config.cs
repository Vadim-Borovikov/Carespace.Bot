using System;
using System.Collections.Generic;
using AbstractBot;
using Carespace.FinanceHelper;
using Newtonsoft.Json;

namespace Carespace.Bot.Config
{
    public class Config : ConfigGoogleSheets
    {
        [JsonProperty]
        public string GoogleCredentialJson { get; set; }

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
        public string DiscussGroupLogin { get; set; }

        [JsonProperty]
        public int ProductId { get; set; }

        [JsonProperty]
        public DateTime SellsStart { get; set; }

        [JsonProperty]
        public string BookPromo { get; set; }

        [JsonProperty]
        public string ErrorPageUrl { get; set; }

        // NEW

        [JsonProperty]
        public string GoogleSheetIdTransactions { get; set; }

        [JsonProperty]
        public string GoogleSheetIdDonations { get; set; }

        [JsonProperty]
        public string GoogleTransactionsCustomRange { get; set; }

        [JsonProperty]
        public string GoogleTransactionsCustomRangeToClear { get; set; }

        [JsonProperty]
        public string GoogleTransactionsFinalRange { get; set; }

        [JsonProperty]
        public string GoogleDonationsRange { get; set; }

        [JsonProperty]
        public string GoogleDonationsCustomRange { get; set; }

        [JsonProperty]
        public string GoogleDonationsCustomRangeToClear { get; set; }

        [JsonProperty]
        public string GoogleDonationSumsRange { get; set; }

        [JsonProperty]
        public string DigisellerProductUrlFormat { get; set; }

        [JsonProperty]
        public string DigisellerSellUrlFormat { get; set; }

        [JsonProperty]
        public int DigisellerId { get; set; }

        [JsonProperty]
        public string DigisellerApiGuid { get; set; }

        [JsonProperty]
        public string DigisellerLogin { get; set; }

        [JsonProperty]
        public string DigisellerPassword { get; set; }

        [JsonProperty]
        public decimal DigisellerFeePercent { get; set; }

        [JsonProperty]
        public string TaxUserAgent { get; set; }

        [JsonProperty]
        public string TaxSourceDeviceId { get; set; }

        [JsonProperty]
        public string TaxSourceType { get; set; }

        [JsonProperty]
        public string TaxAppVersion { get; set; }

        [JsonProperty]
        public string TaxRefreshToken { get; set; }

        [JsonProperty]
        public string TaxProductNameFormat { get; set; }

        [JsonProperty]
        public long TaxPayerId { get; set; }

        [JsonProperty]
        public decimal TaxFeePercent { get; set; }

        [JsonProperty]
        public string PayMasterPaymentUrlFormat { get; set; }

        [JsonProperty]
        public string PayMasterLogin { get; set; }

        [JsonProperty]
        public string PayMasterPassword { get; set; }

        [JsonProperty]
        public string PayMasterSiteAliasDigiseller { get; set; }

        [JsonProperty]
        public string PayMasterSiteAliasDonations { get; set; }

        [JsonProperty]
        public List<string> PayMasterPurposesFormats { get; set; }

        [JsonProperty]
        public Dictionary<Transaction.PayMethod, decimal> PayMasterFeePercents { get; set; }

        [JsonProperty]
        public Dictionary<string, List<Share>> Shares { get; set; }

        internal long? LogsChatId => SuperAdminId;
        internal string Introduction => string.Join('\n', IntroductionLines);
        internal string Schedule => string.Join('\n', ScheduleLines);
    }
}