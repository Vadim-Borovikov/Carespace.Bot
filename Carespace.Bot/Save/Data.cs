using System.Collections.Generic;
using JetBrains.Annotations;

namespace Carespace.Bot.Save;

public sealed class Data
{
    [UsedImplicitly]
    public Dictionary<long, byte> Strikes { get; set; } = new();

    [UsedImplicitly]
    public Dictionary<string, PurchaseInfo> Purchases { get; set; } = new();
}