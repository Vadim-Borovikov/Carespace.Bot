using System.Collections.Generic;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.FinanceHelper.Tests;

internal sealed class Config
{
    public decimal TaxFeePercent { get; set; }

    public decimal DigisellerFeePercent { get; set; }

    public Dictionary<Transaction.PayMethod, decimal> PayMasterFeePercents { get; set; } = null!;

    public Dictionary<string, List<Share>> Shares { get; set; } = null!;
}