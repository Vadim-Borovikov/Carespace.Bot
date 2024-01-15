using System.Collections.Generic;

// ReSharper disable NullableWarningSuppressionIsUsed

namespace Carespace.FinanceHelper.Tests;

internal sealed class Config
{
    public Dictionary<string, List<Share>> Shares { get; set; } = null!;
}