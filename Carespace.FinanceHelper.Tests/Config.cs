using System.Collections.Generic;

namespace Carespace.FinanceHelper.Tests;

internal sealed class Config
{
    public Dictionary<byte, Product> Products { get; init; } = null!;
}