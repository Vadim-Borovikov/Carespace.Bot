using System;

namespace Carespace.FinanceHelper;

public sealed class Share
{
    public readonly string Agent;
    private readonly decimal? _valueAfterLimit;

    public Share(string agent, decimal? valueAfterLimit, string? promo, bool? promoForNet, decimal? limit,
        decimal value)
    {
        Agent = agent;
        _promo = promo;
        _promoForNet = promoForNet;
        _limit = limit;
        _value = value;
        _valueAfterLimit = valueAfterLimit;
    }

    internal decimal Calculate(decimal amount, decimal? net, decimal total, string? promo)
    {
        if (!string.IsNullOrWhiteSpace(_promo))
        {
            if (promo != _promo)
            {
                return 0;
            }
            if (_promoForNet is null || _promoForNet.Value)
            {
                if (net is null)
                {
                    return 0;
                }
                amount = net.Value;
            }
        }

        decimal value = amount * _value;

        if (_limit is null)
        {
            return value;
        }

        decimal beforeLimit = Math.Max(0, _limit.Value - total);
        decimal valueAfterLimit = _valueAfterLimit ?? 0;
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

    private readonly string? _promo;
    private readonly bool? _promoForNet;
    private readonly decimal? _limit;
    private readonly decimal _value;
}