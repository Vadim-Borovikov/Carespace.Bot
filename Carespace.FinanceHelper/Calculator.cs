using System;
using System.Collections.Generic;

namespace Carespace.FinanceHelper;

public static class Calculator
{
    public static void CalculateShares(IEnumerable<Transaction> transactions, Dictionary<byte, Product> products)
    {
        foreach (Transaction transaction in transactions)
        {
            decimal amount = transaction.Amount;
            Product product = products[transaction.ProductId];
            foreach (Share share in product.Shares)
            {
                if (!transaction.Shares.ContainsKey(share.Agent))
                {
                    transaction.Shares.Add(share.Agent, 0);
                }

                decimal value = Round(share.Calculate(amount, transaction.PromoCode));

                transaction.Shares[share.Agent] += value;
                amount -= value;
            }
        }
    }

    private static decimal Round(decimal d) => Math.Round(d, 2);
}