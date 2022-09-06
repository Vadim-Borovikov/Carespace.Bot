using System;
using System.Collections.Generic;
using System.Linq;
using Carespace.Bot.Config;
using Carespace.FinanceHelper;
using GoogleSheetsManager;
using GryphonUtilities;
using Newtonsoft.Json;

namespace Carespace.Bot.Web.Models;

public sealed class ConfigJson : IConvertibleTo<Config.Config>
{
    [JsonProperty]
    public string? Token { get; set; }
    [JsonProperty]
    public string? SystemTimeZoneId { get; set; }
    [JsonProperty]
    public string? DontUnderstandStickerFileId { get; set; }
    [JsonProperty]
    public string? ForbiddenStickerFileId { get; set; }
    [JsonProperty]
    public double? UpdatesPerSecondLimitPrivate { get; set; }
    [JsonProperty]
    public double? UpdatesPerMinuteLimitGroup { get; set; }
    [JsonProperty]
    public double? UpdatesPerSecondLimitGlobal { get; set; }

    [JsonProperty]
    public string? Host { get; set; }
    [JsonProperty]
    public List<string?>? About { get; set; }
    [JsonProperty]
    public List<string?>? ExtraCommands { get; set; }
    [JsonProperty]
    public List<long?>? AdminIds { get; set; }
    [JsonProperty]
    public long? SuperAdminId { get; set; }

    [JsonProperty]
    public string? GoogleCredentialJson { get; set; }
    [JsonProperty]
    public string? ApplicationName { get; set; }
    [JsonProperty]
    public string? GoogleSheetId { get; set; }

    [JsonProperty]
    public string? Template { get; set; }
    [JsonProperty]
    public LinkJson? FeedbackLink { get; set; }
    [JsonProperty]
    public string? GoogleRange { get; set; }
    [JsonProperty]
    public Uri? EventsFormUri { get; set; }
    [JsonProperty]
    public DateTime? EventsUpdateAt { get; set; }
    [JsonProperty]
    public string? SavePath { get; set; }
    [JsonProperty]
    public int? ProductId { get; set; }
    [JsonProperty]
    public string? GoogleSheetIdTransactions { get; set; }
    [JsonProperty]
    public string? GoogleSheetIdDonations { get; set; }
    [JsonProperty]
    public string? GoogleTransactionsCustomRange { get; set; }
    [JsonProperty]
    public string? GoogleTransactionsCustomRangeToClear { get; set; }
    [JsonProperty]
    public string? GoogleTransactionsFinalRange { get; set; }
    [JsonProperty]
    public string? GoogleDonationsRange { get; set; }
    [JsonProperty]
    public string? GoogleDonationsCustomRange { get; set; }
    [JsonProperty]
    public string? GoogleDonationsCustomRangeToClear { get; set; }
    [JsonProperty]
    public string? GoogleDonationSumsRange { get; set; }
    [JsonProperty]
    public string? DigisellerProductUrlFormat { get; set; }
    [JsonProperty]
    public string? DigisellerSellUrlFormat { get; set; }
    [JsonProperty]
    public int? DigisellerId { get; set; }
    [JsonProperty]
    public string? DigisellerApiGuid { get; set; }
    [JsonProperty]
    public string? DigisellerLogin { get; set; }
    [JsonProperty]
    public string? DigisellerPassword { get; set; }
    [JsonProperty]
    public decimal? DigisellerFeePercent { get; set; }
    [JsonProperty]
    public long? TaxPayerId { get; set; }
    [JsonProperty]
    public decimal? TaxFeePercent { get; set; }
    [JsonProperty]
    public string? PayMasterPaymentUrlFormat { get; set; }
    [JsonProperty]
    public string? PayMasterToken { get; set; }
    [JsonProperty]
    public string? PayMasterMerchantIdDigiseller { get; set; }
    [JsonProperty]
    public string? PayMasterMerchantIdDonations { get; set; }
    [JsonProperty]
    public List<string?>? PayMasterPurposesFormats { get; set; }
    [JsonProperty]
    public List<LinkJson?>? Links { get; set; }
    [JsonProperty]
    public List<string?>? ExercisesLinks { get; set; }
    [JsonProperty]
    public long? EventsChannelId { get; set; }
    [JsonProperty]
    public long? DiscussGroupId { get; set; }
    [JsonProperty]
    public string? DiscussGroupLogin { get; set; }
    [JsonProperty]
    public string? BookPromo { get; set; }
    [JsonProperty]
    public Dictionary<Transaction.PayMethod, decimal?>? PayMasterFeePercents { get; set; }
    [JsonProperty]
    public Dictionary<string, List<ShareJson?>?>? Shares { get; set; }
    [JsonProperty]
    public List<string?>? Introduction { get; init; }
    [JsonProperty]
    public List<string?>? Schedule { get; init; }

    [JsonProperty]
    public Uri? ErrorPageUri { get; set; }
    [JsonProperty]
    public string? AdminIdsJson { get; set; }
    [JsonProperty]
    public string? SharesJson { get; set; }
    [JsonProperty]
    public string? CultureInfoName { get; set; }
    [JsonProperty]
    public Dictionary<string, string?>? GoogleCredential { get; set; }

    public Config.Config Convert()
    {
        string token = Token.GetValue(nameof(Token));
        string systemTimeZoneId = SystemTimeZoneId.GetValue(nameof(SystemTimeZoneId));
        string dontUnderstandStickerFileId = DontUnderstandStickerFileId.GetValue(nameof(DontUnderstandStickerFileId));
        string forbiddenStickerFileId = ForbiddenStickerFileId.GetValue(nameof(ForbiddenStickerFileId));

        double updatesPerSecondLimitPrivate =
            UpdatesPerSecondLimitPrivate.GetValue(nameof(UpdatesPerSecondLimitPrivate));
        TimeSpan sendMessagePeriodPrivate = TimeSpan.FromSeconds(1.0 / updatesPerSecondLimitPrivate);

        double updatesPerMinuteLimitGroup = UpdatesPerMinuteLimitGroup.GetValue(nameof(UpdatesPerMinuteLimitGroup));
        TimeSpan sendMessagePeriodGroup = TimeSpan.FromMinutes(1.0 / updatesPerMinuteLimitGroup);

        double updatesPerSecondLimitGlobal = UpdatesPerSecondLimitGlobal.GetValue(nameof(UpdatesPerSecondLimitGlobal));
        TimeSpan sendMessagePeriodGlobal = TimeSpan.FromSeconds(1.0 / updatesPerSecondLimitGlobal);

        string googleCredentialJson = string.IsNullOrWhiteSpace(GoogleCredentialJson)
            ? JsonConvert.SerializeObject(GoogleCredential)
            : GoogleCredentialJson;
        string applicationName = ApplicationName.GetValue(nameof(ApplicationName));
        string googleSheetId = GoogleSheetId.GetValue(nameof(GoogleSheetId));

        string template = Template.GetValue(nameof(Template));
        Link feedbackLink = FeedbackLink.GetValue(nameof(FeedbackLink)).Convert();
        string googleRange = GoogleRange.GetValue(nameof(GoogleRange));
        Uri eventsFormUri = EventsFormUri.GetValue(nameof(EventsFormUri));
        DateTime eventsUpdateAt = EventsUpdateAt.GetValue(nameof(EventsUpdateAt));
        string savePath = SavePath.GetValue(nameof(SavePath));
        int productId = ProductId.GetValue(nameof(ProductId));
        string googleSheetIdTransactions = GoogleSheetIdTransactions.GetValue(nameof(GoogleSheetIdTransactions));
        string googleSheetIdDonations = GoogleSheetIdDonations.GetValue(nameof(GoogleSheetIdDonations));
        string googleTransactionsCustomRange =
            GoogleTransactionsCustomRange.GetValue(nameof(GoogleTransactionsCustomRange));
        string googleTransactionsCustomRangeToClear =
            GoogleTransactionsCustomRangeToClear.GetValue(nameof(GoogleTransactionsCustomRangeToClear));
        string googleTransactionsFinalRange =
            GoogleTransactionsFinalRange.GetValue(nameof(GoogleTransactionsFinalRange));
        string googleDonationsRange = GoogleDonationsRange.GetValue(nameof(GoogleDonationsRange));
        string googleDonationsCustomRange = GoogleDonationsCustomRange.GetValue(nameof(GoogleDonationsCustomRange));
        string googleDonationsCustomRangeToClear =
            GoogleDonationsCustomRangeToClear.GetValue(nameof(GoogleDonationsCustomRangeToClear));
        string googleDonationSumsRange = GoogleDonationSumsRange.GetValue(nameof(GoogleDonationSumsRange));
        string digisellerProductUrlFormat = DigisellerProductUrlFormat.GetValue(nameof(DigisellerProductUrlFormat));
        string digisellerSellUrlFormat = DigisellerSellUrlFormat.GetValue(nameof(DigisellerSellUrlFormat));
        int digisellerId = DigisellerId.GetValue(nameof(DigisellerId));
        string digisellerApiGuid = DigisellerApiGuid.GetValue(nameof(DigisellerApiGuid));
        string digisellerLogin = DigisellerLogin.GetValue(nameof(DigisellerLogin));
        string digisellerPassword = DigisellerPassword.GetValue(nameof(DigisellerPassword));
        decimal digisellerFeePercent = DigisellerFeePercent.GetValue(nameof(DigisellerFeePercent));
        long taxPayerId = TaxPayerId.GetValue(nameof(TaxPayerId));
        decimal taxFeePercent = TaxFeePercent.GetValue(nameof(TaxFeePercent));
        string payMasterPaymentUrlFormat = PayMasterPaymentUrlFormat.GetValue(nameof(PayMasterPaymentUrlFormat));
        string payMasterToken = PayMasterToken.GetValue(nameof(PayMasterToken));
        string payMasterMerchantIdDigiseller =
            PayMasterMerchantIdDigiseller.GetValue(nameof(PayMasterMerchantIdDigiseller));
        string payMasterMerchantIdDonations =
            PayMasterMerchantIdDonations.GetValue(nameof(PayMasterMerchantIdDonations));
        List<string> payMasterPurposesFormats =
            PayMasterPurposesFormats.GetValue(nameof(PayMasterPurposesFormats)).RemoveNulls().ToList();

        List<Link> links = Links.GetValue(nameof(Links)).RemoveNulls().Select(l => l.Convert()).ToList();
        List<string> exercisesLinks = ExercisesLinks.GetValue(nameof(ExercisesLinks)).RemoveNulls().ToList();
        long eventsChannelId = EventsChannelId.GetValue(nameof(EventsChannelId));
        long discussGroupId = DiscussGroupId.GetValue(nameof(DiscussGroupId));
        string discussGroupLogin = DiscussGroupLogin.GetValue(nameof(DiscussGroupLogin));
        string bookPromo = BookPromo.GetValue(nameof(BookPromo));

        Dictionary<Transaction.PayMethod, decimal> payMasterFeePercents =
            PayMasterFeePercents.GetValue(nameof(PayMasterFeePercents))
                                .ToDictionary(p => p.Key, p => p.Value.GetValue());

        if (Shares is null || (Shares.Count == 0))
        {
            string json = SharesJson.GetValue(nameof(SharesJson));
            Shares = JsonConvert.DeserializeObject<Dictionary<string, List<ShareJson?>?>>(json);
        }
        Dictionary<string, List<Share>> shares =
            Shares.GetValue(nameof(Shares)).ToDictionary(p => p.Key,
                p => p.Value.GetValue().RemoveNulls().Select(s => s.Convert()).ToList());

        if (AdminIds is null || (AdminIds.Count == 0))
        {
            string json = AdminIdsJson.GetValue(nameof(AdminIdsJson));
            AdminIds = JsonConvert.DeserializeObject<List<long?>>(json);
        }

        return new Config.Config(token, systemTimeZoneId, dontUnderstandStickerFileId, forbiddenStickerFileId,
            sendMessagePeriodPrivate, sendMessagePeriodGroup, sendMessagePeriodGlobal, googleCredentialJson,
            applicationName, googleSheetId, template, feedbackLink, googleRange, eventsFormUri, eventsUpdateAt,
            savePath, productId, googleSheetIdTransactions, googleSheetIdDonations, googleTransactionsCustomRange,
            googleTransactionsCustomRangeToClear, googleTransactionsFinalRange, googleDonationsRange,
            googleDonationsCustomRange, googleDonationsCustomRangeToClear, googleDonationSumsRange,
            digisellerProductUrlFormat, digisellerSellUrlFormat, digisellerId, digisellerApiGuid, digisellerLogin,
            digisellerPassword, digisellerFeePercent, taxPayerId, taxFeePercent, payMasterPaymentUrlFormat,
            payMasterToken, payMasterMerchantIdDigiseller, payMasterMerchantIdDonations, payMasterPurposesFormats,
            links, exercisesLinks, eventsChannelId, discussGroupId, discussGroupLogin,
            bookPromo, payMasterFeePercents, shares)
        {
            Host = Host,
            About = About is null ? null : string.Join(Environment.NewLine, About),
            ExtraCommands = ExtraCommands is null ? null : string.Join(Environment.NewLine, ExtraCommands),
            AdminIds = AdminIds is null ? new List<long>() : AdminIds.Select(id => id.GetValue("Admin id")).ToList(),
            SuperAdminId = SuperAdminId,
            Introduction = Introduction is null ? null : string.Join(Environment.NewLine, Introduction),
            Schedule = Schedule is null ? null : string.Join(Environment.NewLine, Schedule)
        };
    }
}