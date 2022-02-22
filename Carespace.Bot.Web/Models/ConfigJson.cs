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
    public string? TaxUserAgent { get; set; }
    [JsonProperty]
    public string? TaxSourceDeviceId { get; set; }
    [JsonProperty]
    public string? TaxSourceType { get; set; }
    [JsonProperty]
    public string? TaxAppVersion { get; set; }
    [JsonProperty]
    public string? TaxRefreshToken { get; set; }
    [JsonProperty]
    public string? TaxProductNameFormat { get; set; }
    [JsonProperty]
    public long? TaxPayerId { get; set; }
    [JsonProperty]
    public decimal? TaxFeePercent { get; set; }
    [JsonProperty]
    public string? PayMasterPaymentUrlFormat { get; set; }
    [JsonProperty]
    public string? PayMasterLogin { get; set; }
    [JsonProperty]
    public string? PayMasterPassword { get; set; }
    [JsonProperty]
    public string? PayMasterSiteAliasDigiseller { get; set; }
    [JsonProperty]
    public string? PayMasterSiteAliasDonations { get; set; }
    [JsonProperty]
    public List<string?>? PayMasterPurposesFormats { get; set; }
    [JsonProperty]
    public List<LinkJson?>? Links { get; set; }
    [JsonProperty]
    public List<string?>? ExercisesLinks { get; set; }
    [JsonProperty]
    public string? EventsChannelLogin { get; set; }
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
        string taxUserAgent = TaxUserAgent.GetValue(nameof(TaxUserAgent));
        string taxSourceDeviceId = TaxSourceDeviceId.GetValue(nameof(TaxSourceDeviceId));
        string taxSourceType = TaxSourceType.GetValue(nameof(TaxSourceType));
        string taxAppVersion = TaxAppVersion.GetValue(nameof(TaxAppVersion));
        string taxRefreshToken = TaxRefreshToken.GetValue(nameof(TaxRefreshToken));
        string taxProductNameFormat = TaxProductNameFormat.GetValue(nameof(TaxProductNameFormat));
        long taxPayerId = TaxPayerId.GetValue(nameof(TaxPayerId));
        decimal taxFeePercent = TaxFeePercent.GetValue(nameof(TaxFeePercent));
        string payMasterPaymentUrlFormat = PayMasterPaymentUrlFormat.GetValue(nameof(PayMasterPaymentUrlFormat));
        string payMasterLogin = PayMasterLogin.GetValue(nameof(PayMasterLogin));
        string payMasterPassword = PayMasterPassword.GetValue(nameof(PayMasterPassword));
        string payMasterSiteAliasDigiseller =
            PayMasterSiteAliasDigiseller.GetValue(nameof(PayMasterSiteAliasDigiseller));
        string payMasterSiteAliasDonations = PayMasterSiteAliasDonations.GetValue(nameof(PayMasterSiteAliasDonations));
        List<string> payMasterPurposesFormats =
            PayMasterPurposesFormats.GetValue(nameof(PayMasterPurposesFormats)).RemoveNulls().ToList();

        List<Link> links = Links.GetValue(nameof(Links)).RemoveNulls().Select(l => l.Convert()).ToList();
        List<string> exercisesLinks = ExercisesLinks.GetValue(nameof(ExercisesLinks)).RemoveNulls().ToList();
        string eventsChannelLogin = EventsChannelLogin.GetValue(nameof(EventsChannelLogin));
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
        List<long>? adminIds = AdminIds?.Select(id => id.GetValue("Admin id")).ToList();

        return new Config.Config(token, systemTimeZoneId, dontUnderstandStickerFileId, forbiddenStickerFileId,
            googleCredentialJson, applicationName, googleSheetId, template, feedbackLink, googleRange, eventsFormUri,
            eventsUpdateAt, savePath, productId, googleSheetIdTransactions, googleSheetIdDonations,
            googleTransactionsCustomRange, googleTransactionsCustomRangeToClear, googleTransactionsFinalRange,
            googleDonationsRange, googleDonationsCustomRange, googleDonationsCustomRangeToClear,
            googleDonationSumsRange, digisellerProductUrlFormat, digisellerSellUrlFormat, digisellerId,
            digisellerApiGuid, digisellerLogin, digisellerPassword, digisellerFeePercent, taxUserAgent,
            taxSourceDeviceId, taxSourceType, taxAppVersion, taxRefreshToken, taxProductNameFormat, taxPayerId,
            taxFeePercent, payMasterPaymentUrlFormat, payMasterLogin, payMasterPassword, payMasterSiteAliasDigiseller,
            payMasterSiteAliasDonations, payMasterPurposesFormats, links, exercisesLinks, eventsChannelLogin,
            discussGroupLogin, bookPromo, payMasterFeePercents, shares)
        {
            Host = Host,
            About = About is null ? null : string.Join(Environment.NewLine, About),
            ExtraCommands = ExtraCommands is null ? null : string.Join(Environment.NewLine, ExtraCommands),
            AdminIds = adminIds,
            SuperAdminId = SuperAdminId,
            Introduction = Introduction is null ? null : string.Join(Environment.NewLine, Introduction),
            Schedule = Schedule is null ? null : string.Join(Environment.NewLine, Schedule)
        };
    }
}