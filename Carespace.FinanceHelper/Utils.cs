using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Carespace.FinanceHelper.Data.Digiseller;
using Carespace.FinanceHelper.Data.PayMaster;
using Carespace.FinanceHelper.Providers;
using GryphonUtilities;
using SelfWork;

namespace Carespace.FinanceHelper;

public static class Utils
{
    #region Google

    internal static Transaction.PayMethod? ToPayMathod(this object? o)
    {
        if (o is Transaction.PayMethod p)
        {
            return p;
        }
        return Enum.TryParse(o?.ToString(), out p) ? p : null;
    }

    public static MailAddress? ToEmail(this object? o)
    {
        if (o is MailAddress e)
        {
            return e;
        }

        try
        {
            string? s = o?.ToString();
            return s is null ? null : new MailAddress(s);
        }
        catch
        {
            return null;
        }
    }

    #endregion // Google

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region SelfWork

    public static async Task RegisterTaxesAsync(IEnumerable<Transaction> transactions, string userAgent,
        string sourceDeviceId, string sourceType, string appVersion, string refreshToken, string nameFormat)
    {
        string? token = null;
        foreach (Transaction t in transactions.Where(t => t.Price.HasValue
                                                          && string.IsNullOrWhiteSpace(t.TaxReceiptId)))
        {
            token ??= await DataManager.GetTokenAsync(userAgent, sourceDeviceId, sourceType, appVersion, refreshToken);

            string name = await GetTaxNameAsync(t, nameFormat);
            decimal amount = t.Price.GetValue();

            t.TaxReceiptId = await DataManager.PostIncomeFromIndividualAsync(name, amount, token, t.Date);
        }
    }

    #endregion // SelfWork

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region Digiseller

    public static async Task<List<Transaction>> GetNewDigisellerSellsAsync(string login, string password, int sellerId,
        List<int> productIds, DateTime dateStart, DateTime dateFinish, string sellerSecret,
        IEnumerable<Transaction> oldTransactions)
    {
        List<SellsResponse.Sell> sells =
            await GetDigisellerSellsAsync(sellerId, productIds, dateStart, dateFinish, sellerSecret);

        IEnumerable<int> oldSellIds = oldTransactions.Select(t => t.DigisellerSellId).RemoveNulls();

        IEnumerable<SellsResponse.Sell> newSells =
            sells.Where(s => !s.InvoiceId.HasValue || !oldSellIds.Contains(s.InvoiceId.Value));

        string token = await GetTokenAsync(login, password, sellerSecret);

        List<Transaction> transactions = new();
        foreach (SellsResponse.Sell sell in newSells)
        {
            Transaction transaction = await CreateTransactionAsync(sell, token);
            transactions.Add(transaction);
        }
        return transactions;
    }

    private static async Task<List<SellsResponse.Sell>> GetDigisellerSellsAsync(int sellerId, List<int> productIds,
        DateTime dateStart, DateTime dateFinish, string sellerSecret)
    {
        string start = dateStart.ToString(GoogleDateTimeFormat);
        string end = dateFinish.ToString(GoogleDateTimeFormat);
        int page = 1;
        int totalPages;
        List<SellsResponse.Sell> sells = new();
        do
        {
            SellsResponse dto = await Digiseller.GetSellsAsync(sellerId, productIds, start, end, page, sellerSecret);
            if (dto.Sells is not null)
            {
                sells.AddRange(dto.Sells.RemoveNulls());
            }
            ++page;
            totalPages = dto.Pages.GetValue(nameof(dto.Pages));
        } while (page <= totalPages);
        return sells;
    }

    private static async Task<string> GetTaxNameAsync(Transaction transaction, string taxNameFormat)
    {
        if (transaction.DigisellerProductId is null)
        {
            return transaction.Name ?? "";
        }

        ProductResponse info = await Digiseller.GetProductsInfoAsync(transaction.DigisellerProductId.Value);
        string? productName = info.Product?.Name;
        return string.Format(taxNameFormat, productName);
    }

    private static async Task<Transaction> CreateTransactionAsync(SellsResponse.Sell sell, string token)
    {
        DateTime datePay = sell.DatePay.GetValue(nameof(sell.DatePay));

        decimal amountIn = sell.AmountIn.GetValue(nameof(sell.AmountIn));

        string? promoCode = null;
        if (sell.InvoiceId.HasValue)
        {
            promoCode = await GetPromoCodeAsync(sell.InvoiceId.Value, token);
        }

        MailAddress email = sell.Email.ToEmail().GetValue(nameof(sell.Email));

        Transaction.PayMethod payMethod = sell.PayMethodInfo switch
        {
            SellsResponse.Sell.PayMethod.BankCard => Transaction.PayMethod.BankCard,
            SellsResponse.Sell.PayMethod.Sbp      => Transaction.PayMethod.Sbp,
            _                                     => throw new ArgumentOutOfRangeException(nameof(sell.PayMethodInfo),
                                                                                         sell.PayMethodInfo, null)
        };

        return new Transaction(datePay, sell.ProductName, amountIn, promoCode, sell.InvoiceId, sell.ProductId, email,
            payMethod);
    }

    private static async Task<string> GetTokenAsync(string login, string password, string sellerSecret)
    {
        TokenResponse result = await Digiseller.GetTokenAsync(login, password, sellerSecret);
        return result.Token.GetValue(nameof(result.Token));
    }

    private static async Task<string?> GetPromoCodeAsync(int invoiceId, string token)
    {
        PurchaseResponse result = await Digiseller.GetPurchaseAsync(invoiceId, token);
        PurchaseResponse.ContentInfo content = result.Content.GetValue(nameof(result.Content));
        return content.PromoCode;
    }

    private const string GoogleDateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    #endregion // Digiseller

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region PayMaster

    public static async Task<List<Donation>> GetNewPayMasterPaymentsAsync(string siteAlias, DateTime start, DateTime end,
        string login, string password, IEnumerable<Donation> oldPayments)
    {
        List<ListPaymentsFilterResult.ResponseInfo.Payment> allPayments =
            await GetPaymentsAsync(siteAlias, start, end, login, password);
        List<ListPaymentsFilterResult.ResponseInfo.Payment> payments =
            allPayments.Where(p => p.IsTestPayment is null || !p.IsTestPayment.Value).ToList();

        IEnumerable<int?> oldPaymentIds = oldPayments.Select(p => p.PaymentId);

        IEnumerable<ListPaymentsFilterResult.ResponseInfo.Payment> newPayments =
            payments.Where(p => !oldPaymentIds.Contains(p.PaymentId));

        return newPayments.Select(p => new Donation(p)).ToList();
    }

    internal static string? GetPayMasterHyperlink(int? paymentId)
    {
        return GetHyperlink(PayMasterPaymentUrlFormat, paymentId);
    }

    public static async Task<List<ListPaymentsFilterResult.ResponseInfo.Payment>> GetPaymentsAsync(string siteAlias,
        DateTime start, DateTime end, string login, string password)
    {
        List<ListPaymentsFilterResult.ResponseInfo.Payment> result = new();
        DateTime periodFrom = start;
        while (periodFrom < end)
        {
            DateTime periodTo = Min(periodFrom + PayMasterMaxRequestPeriod, end);

            List<ListPaymentsFilterResult.ResponseInfo.Payment?> payments =
                await GetPaymentsLimitedAsync(siteAlias, periodFrom, periodTo, login, password);
            result.AddRange(payments.RemoveNulls());

            periodFrom = periodTo.AddDays(1);
        }

        return result;
    }

    private static async Task<List<ListPaymentsFilterResult.ResponseInfo.Payment?>> GetPaymentsLimitedAsync(
        string siteAlias, DateTime periodFrom, DateTime periodTo, string login, string password)
    {
        string start = periodFrom.ToString(PayMasterDateTimeFormat);
        string end = periodTo.ToString(PayMasterDateTimeFormat);

        ListPaymentsFilterResult result =
            await PayMaster.GetPaymentsAsync(login, password, "", siteAlias, start, end, "", PayMasterState);

        ListPaymentsFilterResult.ResponseInfo response = result.Response.GetValue(nameof(result.Response));
        return response.Payments.GetValue(nameof(response.Payments));
    }

    public static void FindPayment(Transaction transaction,
        IEnumerable<ListPaymentsFilterResult.ResponseInfo.Payment> payments, IEnumerable<string> purposesFormats)
    {
        if (transaction.DigisellerSellId is null || transaction.PayMasterPaymentId.HasValue)
        {
            return;
        }

        IEnumerable<string> purposes =
            purposesFormats.Select(f => string.Format(f, transaction.DigisellerSellId.Value));

        ListPaymentsFilterResult.ResponseInfo.Payment? payment =
            payments.SingleOrDefault(p => purposes.Contains(p.Purpose));

        transaction.PayMasterPaymentId = payment?.PaymentId;
    }

    private const string PayMasterDateTimeFormat = "yyyy-MM-dd";
    private const string PayMasterState = "COMPLETE";

    #endregion // PayMaster

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

    #region Common

    private static DateTime Min(DateTime dateTime1, DateTime dataTime2)
    {
        return new DateTime(Math.Min(dateTime1.Ticks, dataTime2.Ticks));
    }

    public static void CalculateTotalsAndWeeks(IEnumerable<Donation> donations,
        Dictionary<Transaction.PayMethod, decimal> payMasterFeePercents, DateTime firstThursday)
    {
        foreach (Donation donation in donations)
        {
            decimal payMasterFee = 0;
            if (donation.PayMethodInfo.HasValue)
            {
                decimal percent = payMasterFeePercents[donation.PayMethodInfo.Value];
                payMasterFee = Round(donation.Amount * percent);
            }
            donation.Total = donation.Amount - payMasterFee;

            donation.Week = (ushort) Math.Ceiling((donation.Date - firstThursday).TotalDays / 7);
        }
    }

    public static void CalculateShares(IEnumerable<Transaction> transactions, decimal taxFeePercent,
        decimal digisellerFeePercent, Dictionary<Transaction.PayMethod, decimal> payMasterFeePercents,
        Dictionary<string, List<Share>> shares)
    {
        Dictionary<string, decimal> totals = new();
        foreach (Transaction transaction in transactions)
        {
            decimal amount = transaction.Amount;

            decimal? net = null;
            if (transaction.Price.HasValue)
            {
                decimal price = transaction.Price.Value;

                // Tax
                decimal tax = Round(price * taxFeePercent);
                transaction.Tax = tax;
                amount -= transaction.Tax.Value;

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

    public static int? ExtractIntParameter(string value, string format)
    {
        string? paramter = ExtractParameter(value, format);
        return int.TryParse(paramter, out int result) ? result : null;
    }

    public static string? ExtractParameter(string value, string format)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        int left = format.IndexOf('{');
        int right = format.IndexOf('}');

        int prefixLength = left;
        int postfixLenght = format.Length - right - 1;

        return value.Substring(prefixLength, value.Length - prefixLength - postfixLenght);
    }

    internal static string? GetHyperlink(string urlFormat, object? parameter)
    {
        string? caption = parameter?.ToString();
        if (string.IsNullOrWhiteSpace(caption))
        {
            return null;
        }
        string url = string.Format(urlFormat, caption);
        Uri uri = new(url);
        return GetHyperlink(uri, caption);
    }

    internal static string GetHyperlink(Uri uri, string? caption)
    {
        if (string.IsNullOrWhiteSpace(caption))
        {
            caption = uri.AbsoluteUri;
        }
        return string.Format(HyperlinkFormat, uri.AbsoluteUri, caption);
    }

    private static decimal Round(decimal d) => Math.Round(d, 2);

    private const string HyperlinkFormat = "=HYPERLINK(\"{0}\";\"{1}\")";
    private const string NoProductSharesKey = "None";

    public static string PayMasterPaymentUrlFormat = "";

    private static readonly TimeSpan PayMasterMaxRequestPeriod = TimeSpan.FromDays(179);

    #endregion // Common
}