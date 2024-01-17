using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Carespace.Bot;

[PublicAPI]
internal sealed class Purchase
{
    public string ClientName { get; init; } = null!;

    public DateOnly Date { get; init; }

    public List<Item> Items { get; } = new();
}