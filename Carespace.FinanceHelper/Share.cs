using System;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.FinanceHelper;

[PublicAPI]
public sealed class Share
{
    [Required]
    [MinLength(1)]
    public string Agent { get; init; } = null!;

    public string? Promo { get; init; }

    [Required]
    public decimal Value { get; init; }

    public decimal Calculate(decimal amount, string? promo)
    {
        if (!string.IsNullOrWhiteSpace(Promo) && !string.Equals(promo, Promo, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return amount * Value;
    }
}