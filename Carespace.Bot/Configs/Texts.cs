// ReSharper disable NullableWarningSuppressionIsUsed

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AbstractBot.Configs;

namespace Carespace.Bot.Configs;

[PublicAPI]
public class Texts : AbstractBot.Configs.Texts
{
    [Required]
    [MinLength(1)]
    public string ExercisesCommandDescription { get; init; } = null!;
    [Required]
    [MinLength(1)]
    public List<Uri> ExerciseUris { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string FeedbackCommandDescription { get; init; } = null!;
    [Required]
    public Link FeedbackLink { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string IntroCommandDescription { get; init; } = null!;
    [Required]
    public MessageTemplate PracticeIntroduction { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string LinksCommandDescription { get; init; } = null!;
    [Required]
    [MinLength(1)]
    public List<Link> Links { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string ScheduleCommandDescription { get; init; } = null!;
    [Required]
    public MessageTemplate PracticeSchedule { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string SpamCommandDescription { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string WarningCommandDescription { get; init; } = null!;

    [Required]
    public Uri ChatGuidelinesUri { get; init; } = null!;

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
    public MessageTemplate RestrictionMessageFormat { get; init; } = null!;

    [Required]
    public MessageTemplate CheckingEmail { get; init; } = null!;
    [Required]
    public MessageTemplate EmailFoundFormat { get; init; } = null!;
    [Required]
    public MessageTemplate EmailNotFoundFormat { get; init; } = null!;
    [Required]
    public MessageTemplate EmailNotFoundHelp { get; init; } = null!;

    [Required]
    public MessageTemplate PaymentConfirmationFormat { get; init; } = null!;

    [Required]
    public MessageTemplate ListItemFormat { get; init; } = null!;

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
    public MessageTemplate AddingPurchases { get; init; } = null!;
}