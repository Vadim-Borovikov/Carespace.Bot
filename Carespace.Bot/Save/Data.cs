using System.Collections.Generic;
using JetBrains.Annotations;

namespace Carespace.Bot.Save;

public sealed class Data
{
    [UsedImplicitly]
    public Dictionary<long, byte> Strikes { get; set; } = new();
}