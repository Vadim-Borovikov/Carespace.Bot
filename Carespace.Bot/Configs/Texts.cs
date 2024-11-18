using JetBrains.Annotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AbstractBot.Configs;
using AbstractBot.Configs.MessageTemplates;

namespace Carespace.Bot.Configs;

[PublicAPI]
public class Texts : AbstractBot.Configs.Texts
{
    [Required]
    [MinLength(1)]
    public string AdminCommandDescription { get; init; } = null!;

    [Required]
    public MessageTemplateText AdminCommandReaction { get; init; } = null!;
    [Required]
    public MessageTemplateText AdminCommandPingFormat { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string FeedbackCommandDescription { get; init; } = null!;
    [Required]
    public Link FeedbackLink { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string LinksCommandDescription { get; init; } = null!;
    [Required]
    [MinLength(1)]
    public List<Link> Links { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string SpamCommandDescription { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string WarningCommandDescription { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string DaysFormat { get; init; } = null!;

    [Required]
    public Noun Day { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string RestrictionWarningPartFormat { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string RestrictionPartFormat { get; init; } = null!;

    [Required]
    public MessageTemplateText RestrictionMessageFormat { get; init; } = null!;

    [Required]
    public MessageTemplateText CheckingEmail { get; init; } = null!;
    [Required]
    public MessageTemplateText EmailFoundFormat { get; init; } = null!;
    [Required]
    public MessageTemplateText EmailNotFoundFormat { get; init; } = null!;
    [Required]
    public MessageTemplateText EmailNotFoundHelp { get; init; } = null!;

    [Required]
    public MessageTemplateText PaymentConfirmationFormat { get; init; } = null!;

    [Required]
    public MessageTemplateText ListItemFormat { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string ProductSoldNoteFormat { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string PaymentSlipButtonCaption { get; init; } = null!;
    [Required]
    [MinLength(1)]
    public string PaymentSlipButtonFormat { get; init; } = null!;
    [Required]
    [MinLength(1)]
    public string PaymentConfirmationButton { get; init; } = null!;

    [Required]
    public MessageTemplateText AddingPurchases { get; init; } = null!;

    [Required]
    public MessageTemplateText MessageForClientFormat { get; init; } = null!;
    [Required]
    public MessageTemplateText CopyableFormat { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public Dictionary<byte, MessageTemplateFile> ProductMessages { get; init; } = null!;

    [Required]
    public MessageTemplateText ThankYou { get; init; } = null!;
}