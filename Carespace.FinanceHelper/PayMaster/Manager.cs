using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Carespace.FinanceHelper.Data.PayMaster;
using GryphonUtilities.Extensions;

namespace Carespace.FinanceHelper.PayMaster;

public static class Manager
{
    public static string PaymentUrlFormat = "";

    public static async Task<List<Donation>> GetNewPaymentsAsync(string merchantId, DateOnly start, DateOnly end,
        string token, IEnumerable<Donation> oldPayments, JsonSerializerOptions options)
    {
        List<PaymentsResult.Item> allPayments = await GetPaymentsAsync(merchantId, start, end, token, options);
        List<PaymentsResult.Item> payments = allPayments.Where(p => p.TestMode is null || !p.TestMode.Value).ToList();

        IEnumerable<string?> oldPaymentIds = oldPayments.Select(p => p.PaymentId);

        IEnumerable<PaymentsResult.Item> newPayments = payments.Where(p => !oldPaymentIds.Contains(p.Id));

        return newPayments.Select(p => new Donation(p)).ToList();
    }

    public static async Task<List<PaymentsResult.Item>> GetPaymentsAsync(string merchantId, DateOnly start,
        DateOnly end, string token, JsonSerializerOptions options)
    {
        string startFormatted = start.ToString(DateTimeFormat);
        string endFormatted = end.ToString(DateTimeFormat);

        PaymentsResult result =
            await Provider.GetPaymentsAsync(token, merchantId, startFormatted, endFormatted, options);
        List<PaymentsResult.Item?> items = result.Items.GetValue(nameof(result.Items));
        return items.RemoveNulls().Where(p => p.Status == Status).ToList();
    }

    public static void FindPayment(Transaction transaction, IEnumerable<PaymentsResult.Item> payments,
        IEnumerable<string> descriptionFormats)
    {
        if (transaction.DigisellerSellId is null || transaction.PayMasterPaymentId is not null)
        {
            return;
        }

        IEnumerable<string> descriptions =
            descriptionFormats.Select(f => string.Format(f, transaction.DigisellerSellId.Value));

        PaymentsResult.Item? payment = payments.SingleOrDefault(p => !string.IsNullOrWhiteSpace(p.Invoice?.Description)
                                                                     && descriptions.Contains(p.Invoice.Description));

        transaction.PayMasterPaymentId = payment?.Id;
    }

    internal static string? GetHyperlink(string? paymentId) => Hyperlink.From(PaymentUrlFormat, paymentId);

    private const string DateTimeFormat = "yyyy-MM-dd";
    private const string Status = "Settled";
}