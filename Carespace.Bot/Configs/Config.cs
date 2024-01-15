using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using AbstractBot.Configs;
using Carespace.FinanceHelper;
using JetBrains.Annotations;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.Bot.Configs;

[PublicAPI]
public class Config : ConfigWithSheets<Texts>
{
    [Required]
    [MinLength(1)]
    public string InstantViewFormat { get; init; } = null!;

    [Required]
    public int ProductId { get; init; }

    [Required]
    [MinLength(1)]
    public string GoogleSheetIdTransactions { get; init; } = null!;

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
    public string DigisellerProductUrlFormat { get; init; } = null!;

    [Required]
    public long DiscussGroupId { get; init; }

    [Required]
    [MinLength(1)]
    public string DiscussGroupLogin { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string BookPromo { get; init; } = null!;

    [MinLength(1)]
    public Dictionary<string, List<Share>>? Shares { get; init; }

    public string? SharesJson { get; init; }

    public Dictionary<string, List<Share>>? GetShares(JsonSerializerOptions options)
    {
        if (Shares is not null)
        {
            return Shares;
        }

        return SharesJson is null
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, List<Share>>>(SharesJson, options);
    }

    public byte InitialStrikesForSpammers { get; init; }

    public ushort RestrictionsMaxDays { get; init; }
}