using System;
using System.Collections.Generic;

namespace Carespace.FinanceHelper;

public static class Calculator
{
    public static void CalculateShares(IEnumerable<Transaction> transactions, Dictionary<string, List<Share>> shares)
    {
        Dictionary<string, decimal> totals = new();
        foreach (Transaction transaction in transactions)
        {
            decimal amount = transaction.Amount;

            string product = transaction.DigisellerProductId?.ToString() ?? NoProductSharesKey;
            foreach (Share share in shares[product])
            {
                if (!transaction.Shares.ContainsKey(share.Agent))
                {
                    transaction.Shares.Add(share.Agent, 0);
                }

                if (!totals.ContainsKey(share.Agent))
                {
                    totals.Add(share.Agent, 0);
                }

                decimal value = Round(share.Calculate(amount, totals[share.Agent], transaction.PromoCode));

                transaction.Shares[share.Agent] += value;
                totals[share.Agent] += value;
                amount -= value;
            }
        }
    }

    private static decimal Round(decimal d) => Math.Round(d, 2);

    private const string NoProductSharesKey = "None";
}