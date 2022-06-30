using System;
using System.Collections.Generic;
using AbstractBot;
using Carespace.FinanceHelper;

namespace Carespace.Bot.Config;

public sealed class Config : ConfigGoogleSheets
{
    public readonly string Template;
    public readonly Link FeedbackLink;
    public readonly string GoogleRange;
    public readonly Uri EventsFormUri;
    public readonly DateTime EventsUpdateAt;
    public readonly string SavePath;
    public readonly int ProductId;
    public readonly string GoogleSheetIdTransactions;
    public readonly string GoogleSheetIdDonations;
    public readonly string GoogleTransactionsCustomRange;
    public readonly string GoogleTransactionsCustomRangeToClear;
    public readonly string GoogleTransactionsFinalRange;
    public readonly string GoogleDonationsRange;
    public readonly string GoogleDonationsCustomRange;
    public readonly string GoogleDonationsCustomRangeToClear;
    public readonly string GoogleDonationSumsRange;
    public readonly string DigisellerProductUrlFormat;
    public readonly string DigisellerSellUrlFormat;
    public readonly int DigisellerId;
    public readonly string DigisellerApiGuid;
    public readonly string DigisellerLogin;
    public readonly string DigisellerPassword;
    public readonly decimal DigisellerFeePercent;
    public readonly long TaxPayerId;
    public readonly decimal TaxFeePercent;
    public readonly string PayMasterPaymentUrlFormat;
    public readonly string PayMasterLogin;
    public readonly string PayMasterPassword;
    public readonly string PayMasterSiteAliasDigiseller;
    public readonly string PayMasterSiteAliasDonations;
    public readonly List<string> PayMasterPurposesFormats;

    internal readonly List<Link> Links;
    internal readonly List<string> ExercisesLinks;
    internal readonly long EventsChannelId;
    internal readonly long DiscussGroupId;
    internal readonly string DiscussGroupLogin;
    internal readonly string BookPromo;
    internal readonly Dictionary<Transaction.PayMethod, decimal> PayMasterFeePercents;
    internal readonly Dictionary<string, List<Share>> Shares;

    public string? Introduction { internal get; init; }
    public string? Schedule { internal get; init; }

    internal long? LogsChatId => SuperAdminId;

    public Config(string token, string systemTimeZoneId, string dontUnderstandStickerFileId,
        string forbiddenStickerFileId, TimeSpan sendMessagePeriodPrivate, TimeSpan sendMessagePeriodGroup,
        TimeSpan sendMessageDelayGlobal, string googleCredentialJson, string applicationName, string googleSheetId,
        string template, Link feedbackLink, string googleRange, Uri eventsFormUri, DateTime eventsUpdateAt,
        string savePath, int productId, string googleSheetIdTransactions, string googleSheetIdDonations,
        string googleTransactionsCustomRange, string googleTransactionsCustomRangeToClear,
        string googleTransactionsFinalRange, string googleDonationsRange, string googleDonationsCustomRange,
        string googleDonationsCustomRangeToClear, string googleDonationSumsRange, string digisellerProductUrlFormat,
        string digisellerSellUrlFormat, int digisellerId, string digisellerApiGuid, string digisellerLogin,
        string digisellerPassword, decimal digisellerFeePercent, long taxPayerId, decimal taxFeePercent,
        string payMasterPaymentUrlFormat, string payMasterLogin, string payMasterPassword,
        string payMasterSiteAliasDigiseller, string payMasterSiteAliasDonations, List<string> payMasterPurposesFormats,
        List<Link> links, List<string> exercisesLinks, long eventsChannelId, long discussGroupId,
        string discussGroupLogin, string bookPromo, Dictionary<Transaction.PayMethod, decimal> payMasterFeePercents,
        Dictionary<string, List<Share>> shares)
        : base(token, systemTimeZoneId, dontUnderstandStickerFileId, forbiddenStickerFileId, sendMessagePeriodPrivate,
            sendMessagePeriodGroup, sendMessageDelayGlobal, googleCredentialJson, applicationName, googleSheetId)
    {
        Template = template;
        FeedbackLink = feedbackLink;
        GoogleRange = googleRange;
        EventsFormUri = eventsFormUri;
        EventsUpdateAt = eventsUpdateAt;
        SavePath = savePath;
        ProductId = productId;
        GoogleSheetIdTransactions = googleSheetIdTransactions;
        GoogleSheetIdDonations = googleSheetIdDonations;
        GoogleTransactionsCustomRange = googleTransactionsCustomRange;
        GoogleTransactionsCustomRangeToClear = googleTransactionsCustomRangeToClear;
        GoogleTransactionsFinalRange = googleTransactionsFinalRange;
        GoogleDonationsRange = googleDonationsRange;
        GoogleDonationsCustomRange = googleDonationsCustomRange;
        GoogleDonationsCustomRangeToClear = googleDonationsCustomRangeToClear;
        GoogleDonationSumsRange = googleDonationSumsRange;
        DigisellerProductUrlFormat = digisellerProductUrlFormat;
        DigisellerSellUrlFormat = digisellerSellUrlFormat;
        DigisellerId = digisellerId;
        DigisellerApiGuid = digisellerApiGuid;
        DigisellerLogin = digisellerLogin;
        DigisellerPassword = digisellerPassword;
        DigisellerFeePercent = digisellerFeePercent;
        TaxPayerId = taxPayerId;
        TaxFeePercent = taxFeePercent;
        PayMasterPaymentUrlFormat = payMasterPaymentUrlFormat;
        PayMasterLogin = payMasterLogin;
        PayMasterPassword = payMasterPassword;
        PayMasterSiteAliasDigiseller = payMasterSiteAliasDigiseller;
        PayMasterSiteAliasDonations = payMasterSiteAliasDonations;
        PayMasterPurposesFormats = payMasterPurposesFormats;

        Links = links;
        ExercisesLinks = exercisesLinks;
        EventsChannelId = eventsChannelId;
        DiscussGroupId = discussGroupId;
        DiscussGroupLogin = discussGroupLogin;
        BookPromo = bookPromo;
        PayMasterFeePercents = payMasterFeePercents;
        Shares = shares;
    }
}
