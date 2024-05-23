using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AbstractBot.Configs;
using Carespace.FinanceHelper;
using JetBrains.Annotations;

namespace Carespace.Bot.Configs;

[PublicAPI]
public class Config : ConfigWithSheets<Texts>
{
    [Required]
    [MinLength(1)]
    public string InstantViewFormat { get; init; } = null!;

    [Required]
    public byte ProductId { get; init; }

    [Required]
    [MinLength(1)]
    public string GoogleSheetIdTransactions { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleTitle { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GoogleRange { get; init; } = null!;

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
    public Dictionary<byte, Product> Products { get; init; } = null!;

    public byte InitialStrikesForSpammers { get; init; }

    public ushort RestrictionsMaxDays { get; init; }

    [Required]
    public long ItemVendorId { get; init; }

    [Required]
    public string FallbackAgent { get; init; } = null!;

    [Required]
    public Uri PostPurchaseUri { get; init; } = null!;
    [Required]
    [MinLength(1)]
    public string PostPurchaseResource { get; init; } = null!;

    [Required]
    public double RestrictionMessagesLifetimeMinutes { get; init; }
}