using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Carespace.FinanceHelper.Data.Digiseller;
using GryphonUtilities.Extensions;
using GryphonUtilities.Time;

namespace Carespace.FinanceHelper.Digiseller;

public static class Manager
{
    public static async Task<List<Transaction>> GetNewSellsAsync(string login, string password, int sellerId,
        List<int> productIds, DateOnly dateStart, DateOnly dateFinish, string sellerSecret,
        IEnumerable<Transaction> oldTransactions, Clock clock, JsonSerializerOptions options)
    {
        List<SellsResponse.Sell> sells = await GetSellsAsync(sellerId, productIds,
            clock.GetDateTimeFull(dateStart, TimeOnly.MinValue), clock.GetDateTimeFull(dateFinish, TimeOnly.MinValue),
            sellerSecret, options);

        IEnumerable<int> oldSellIds = oldTransactions.Select(t => t.DigisellerSellId).SkipNulls();

        IEnumerable<SellsResponse.Sell> newSells =
            sells.Where(s => !s.InvoiceId.HasValue || !oldSellIds.Contains(s.InvoiceId.Value));

        string token = await GetTokenAsync(login, password, sellerSecret, clock.Now(), options);

        List<Transaction> transactions = new();
        foreach (SellsResponse.Sell sell in newSells)
        {
            Transaction transaction = await CreateTransactionAsync(sell, token, options);
            transactions.Add(transaction);
        }
        return transactions;
    }

    private static async Task<List<SellsResponse.Sell>> GetSellsAsync(int sellerId, List<int> productIds,
        DateTimeFull dateStart, DateTimeFull dateFinish, string sellerSecret, JsonSerializerOptions options)
    {
        string start = dateStart.ToString(GoogleDateTimeFormat);
        string end = dateFinish.ToString(GoogleDateTimeFormat);
        int page = 1;
        int totalPages;
        List<SellsResponse.Sell> sells = new();
        do
        {
            SellsResponse dto =
                await Provider.GetSellsAsync(sellerId, productIds, start, end, page, sellerSecret, options);
            if (dto.Sells is not null)
            {
                sells.AddRange(dto.Sells.SkipNulls());
            }
            ++page;
            totalPages = dto.Pages.Denull(nameof(dto.Pages));
        } while (page <= totalPages);
        return sells;
    }

    private static async Task<Transaction> CreateTransactionAsync(SellsResponse.Sell sell, string token,
        JsonSerializerOptions options)
    {
        DateTimeFull datePay = sell.DatePay.Denull(nameof(sell.DatePay));

        decimal amountIn = sell.AmountIn.Denull(nameof(sell.AmountIn));

        string? promoCode = null;
        if (sell.InvoiceId.HasValue)
        {
            promoCode = await GetPromoCodeAsync(sell.InvoiceId.Value, token, options);
        }

        Transaction.PayMethod payMethod = sell.PayMethodInfo switch
        {
            SellsResponse.Sell.PayMethod.BankCard => Transaction.PayMethod.BankCard,
            SellsResponse.Sell.PayMethod.Sbp => Transaction.PayMethod.Sbp,
            _ => throw new ArgumentOutOfRangeException(nameof(sell.PayMethodInfo), sell.PayMethodInfo, null)
        };

        return new Transaction
        {
            Date = datePay.DateOnly,
            Name = sell.ProductName,
            Amount = amountIn,
            Price = amountIn,
            PromoCode = promoCode,
            DigisellerSellId = sell.InvoiceId,
            DigisellerProductId = sell.ProductId,
            PayMethodInfo = payMethod,
            Email = sell.Email.ToEmail().Denull(nameof(sell.Email))
        };
    }

    private static async Task<string> GetTokenAsync(string login, string password, string sellerSecret,
        DateTimeFull now, JsonSerializerOptions options)
    {
        TokenResponse result = await Provider.GetTokenAsync(login, password, sellerSecret, now, options);
        return result.Token.Denull(nameof(result.Token));
    }

    private static async Task<string?> GetPromoCodeAsync(int invoiceId, string token, JsonSerializerOptions options)
    {
        PurchaseResponse result = await Provider.GetPurchaseAsync(invoiceId, token, options);
        PurchaseResponse.ContentInfo content = result.Content.Denull(nameof(result.Content));
        return content.PromoCode;
    }

    private const string GoogleDateTimeFormat = "yyyy-MM-dd HH:mm:ss";
}