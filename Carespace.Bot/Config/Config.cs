using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AbstractBot.Configs;
using Carespace.FinanceHelper;
using JetBrains.Annotations;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.Bot.Config;

[PublicAPI]
public class Config : ConfigGoogleSheets
{
    [Required]
    [MinLength(1)]
    public string GoogleSheetId { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleTitle { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleRange { get; init; } = null!;

    [Required]
    public Uri EventsFormUri { get; init; } = null!;

    [Required]
    public TimeOnly EventsUpdateAt { get; init; }

    [Required]
    [MinLength(1)]
    public string SavePath { get; init; } = null!;

    [Required]
    public long EventsChannelId { get; init; }

    [Required]
    [MinLength(1)]
    public string Template { get; init; } = null!;

    [Required]
    public Link FeedbackLink { get; init; } = null!;

    [Required]
    public int ProductId { get; init; }

    [Required]
    [MinLength(1)]
    public string GoogleSheetIdTransactions { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleSheetIdDonations { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleCustomTransactionsTitle { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleCustomTransactionsRange { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleCustomTransactionsRangeToClear { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleAllTransactionsTitle { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleAllTransactionsFinalRange { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleAllDonationsTitle { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleAllDonationsRange { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleCustomDonationsTitle { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleCustomDonationsRange { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleCustomDonationsRangeToClear { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleDonationSumsTitle { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleDonationSumsRange { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string DigisellerProductUrlFormat { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string DigisellerSellUrlFormat { get; init; } = null!;

    [Required]
    public int DigisellerId { get; init; }

    [Required]
    [MinLength(1)]
    public string DigisellerApiGuid { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string DigisellerLogin { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string DigisellerPassword { get; init; } = null!;

    [Required]
    public decimal DigisellerFeePercent { get; init; }

    [Required]
    public long TaxPayerId { get; init; }

    [Required]
    public decimal TaxFeePercent { get; init; }

    [Required]
    [MinLength(1)]
    public string PayMasterPaymentUrlFormat { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string PayMasterToken { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string PayMasterMerchantIdDigiseller { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string PayMasterMerchantIdDonations { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public List<string> PayMasterPurposesFormats { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public List<Link> Links { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public List<string> ExercisesLinks { get; init; } = null!;

    [Required]
    public long DiscussGroupId { get; init; }

    [Required]
    [MinLength(1)]
    public string DiscussGroupLogin { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string BookPromo { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public Dictionary<Transaction.PayMethod, decimal> PayMasterFeePercents { get; init; } = null!;

    [MinLength(1)]
    public Dictionary<string, List<Share>>? Shares { get; init; }

    public string? SharesJson { get; init; }

    [Required]
    public List<string> PracticeIntroduction { get; init; } = null!;

    [Required]
    public List<string> PracticeSchedule { get; init; } = null!;

    [Required]
    public string MailPassword { get; init; } = null!;
    [Required]
    public string MailImapHost { get; init; } = null!;
    [Required]
    public int MailImapPort { get; init; }
    [Required]
    public string MailSmtpHost { get; init; } = null!;
    [Required]
    public int MailSmtpPort { get; init; }
    [Required]
    public string MailFromAddress { get; init; } = null!;
    [Required]
    public string MailFromName { get; init; } = null!;
    [Required]
    public Uri MailUri { get; init; } = null!;
    [Required]
    public List<string> MailTextBodyFormatLines { get; init; } = null!;
    [Required]
    public string MailHtmlAddressFormat { get; init; } = null!;
    [Required]
    public List<string> MailHtmlBodyFormatLines { get; init; } = null!;
    [Required]
    public string MailReplyPrefix { get; init; } = null!;
    [Required]
    public string MailAttachmentName { get; init; } = null!;
    [Required]
    public List<string> MailTextFormatLines { get; init; } = null!;
}