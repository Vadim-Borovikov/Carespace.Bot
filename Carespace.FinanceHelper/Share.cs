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

    public bool? PromoForNet { get; init; }

    public decimal? Limit { get; init; }

    [Required]
    public decimal Value { get; init; }

    public decimal? ValueAfterLimit { get; init; }

    public decimal Calculate(decimal amount, decimal total, string? promo)
    {
        if (!string.IsNullOrWhiteSpace(Promo))
        {
            if (promo != Promo)
            {
                return 0;
            }
            if (PromoForNet is null || PromoForNet.Value)
            {
                return 0;
            }
        }

        decimal value = amount * Value;

        if (Limit is null)
        {
            return value;
        }

        decimal beforeLimit = Math.Max(0, Limit.Value - total);
        decimal valueAfterLimit = ValueAfterLimit ?? 0;
        if (beforeLimit == 0)
        {
            return amount * valueAfterLimit;
        }

        if (value < 0)
        {
            return value;
        }

        decimal beforeLimitPart = Math.Min(value, beforeLimit);
        decimal afterLimitPart = valueAfterLimit * Math.Max(0, value - beforeLimitPart);
        return beforeLimitPart + afterLimitPart;
    }
}