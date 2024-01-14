using System;
using System.Collections.Generic;
using GryphonUtilities.Extensions;

namespace Carespace.FinanceHelper;

public static class Calculator
{
    public static void CalculateShares(IEnumerable<Transaction> transactions, decimal taxFeePercent,
        decimal digisellerFeePercent, Dictionary<Transaction.PayMethod, decimal> payMasterFeePercents,
        Dictionary<string, List<Share>> shares)
    {
        Dictionary<string, decimal> totals = new();
        DateOnly doomday = new(2022, 2, 24);
        foreach (Transaction transaction in transactions)
        {
            decimal amount = transaction.Amount;

            decimal? net = null;
            if (transaction.Price.HasValue)
            {
                decimal price = transaction.Price.Value;

                // Tax
                if (transaction.Date <= doomday)
                {
                    decimal tax = Round(price * taxFeePercent);
                    transaction.Tax = tax;
                    amount -= transaction.Tax.Value;
                }

                net = amount;

                if (transaction.DigisellerSellId.HasValue)
                {
                    // Digiseller
                    decimal digisellerFee = Round(price * digisellerFeePercent);
                    transaction.DigisellerFee = digisellerFee;
                    amount -= digisellerFee;

                    // PayMaster
                    Transaction.PayMethod method = transaction.PayMethodInfo.GetValue();
                    decimal percent = payMasterFeePercents[method];
                    decimal payMasterFee = Round(price * percent);
                    transaction.PayMasterFee = payMasterFee;
                    amount -= payMasterFee;
                }
            }

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

                decimal value = Round(share.Calculate(amount, net, totals[share.Agent], transaction.PromoCode));

                transaction.Shares[share.Agent] += value;
                totals[share.Agent] += value;
                amount -= value;
            }
        }
    }

    private static decimal Round(decimal d) => Math.Round(d, 2);

    private const string NoProductSharesKey = "None";
}