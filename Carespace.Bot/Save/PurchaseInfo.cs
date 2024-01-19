using JetBrains.Annotations;
using System.Collections.Generic;

namespace Carespace.Bot.Save;

public sealed class PurchaseInfo
{
    [UsedImplicitly]
    public string Name { get; set; } = null!;
    [UsedImplicitly]
    public string Email { get; set; } = null!;
    [UsedImplicitly]
    public string Telegram { get; set; } = null!;
    [UsedImplicitly]
    public List<byte> ProductIds { get; set; } = null!;
}