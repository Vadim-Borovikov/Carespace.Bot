using System;
using System.Collections.Generic;
using Carespace.FinanceHelper;
using JetBrains.Annotations;

namespace Carespace.Bot;

[PublicAPI]
internal sealed class Item
{
    public string Name { get; }

    public decimal Price { get; }

    public Dictionary<string, decimal> Shares { get; }

    public Item(Transaction transaction, string fallbackAgent)
    {
        Name = transaction.Name!;
        Price = transaction.Amount;
        Shares = new Dictionary<string, decimal>();
        foreach (string agent in transaction.Shares.Keys)
        {
            if (!agent.Equals(fallbackAgent, StringComparison.OrdinalIgnoreCase) && (transaction.Shares[agent] > 0))
            {
                Shares[agent] = transaction.Shares[agent];
            }
        }
    }
}