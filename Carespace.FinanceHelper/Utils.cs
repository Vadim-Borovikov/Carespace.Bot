using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Carespace.FinanceHelper.Dto.Digiseller;
using Carespace.FinanceHelper.Dto.PayMaster;
using Carespace.FinanceHelper.Providers;
using SelfWork;
using TokenResult = Carespace.FinanceHelper.Dto.Digiseller.TokenResult;

namespace Carespace.FinanceHelper
{
    public static class Utils
    {
        #region Google

        internal static Transaction.PayMethod? ToPayMathod(this object o)
        {
            return Enum.TryParse(o?.ToString(), out Transaction.PayMethod p) ? (Transaction.PayMethod?)p : null;
        }

        #endregion // Google

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region SelfWork

        public static async Task RegisterTaxesAsync(IEnumerable<Transaction> transactions, string userAgent,
            string sourceDeviceId, string sourceType, string appVersion, string refreshToken, string nameFormat)
        {
            string token = null;
            foreach (Transaction t in transactions.Where(t => t.Price.HasValue
                                                              && string.IsNullOrWhiteSpace(t.TaxReceiptId)))
            {
                if (token == null)
                {
                    token = await DataManager.GetTokenAsync(userAgent, sourceDeviceId, sourceType, appVersion, refreshToken);
                }

                string name = await GetTaxNameAsync(t, nameFormat);
                // ReSharper disable once PossibleInvalidOperationException
                decimal amount = t.Price.Value;

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
            List<SellsResult.Sell> sells =
                await GetDigisellerSellsAsync(sellerId, productIds, dateStart, dateFinish, sellerSecret);

            IEnumerable<int> oldSellIds = oldTransactions
                .Where(t => t.DigisellerSellId.HasValue)
                .Select(t => t.DigisellerSellId.Value);

            IEnumerable<SellsResult.Sell> newSells = sells.Where(s => !oldSellIds.Contains(s.InvoiceId));

            string token = await GetTokenAsync(login, password, sellerSecret);

            var transactions = new List<Transaction>();
            foreach (SellsResult.Sell sell in newSells)
            {
                Transaction transaction = await CreateTransactionAsync(sell, token);
                transactions.Add(transaction);
            }
            return transactions;
        }

        public static async Task<IEnumerable<string>> GetDigisellerSellsEmailsAsync(int sellerId, List<int> productIds,
            DateTime dateStart, DateTime dateFinish, string sellerSecret)
        {
            List<SellsResult.Sell> sells =
                await GetDigisellerSellsAsync(sellerId, productIds, dateStart, dateFinish, sellerSecret);
            return sells.Select(s => s.Email);
        }

        private static async Task<List<SellsResult.Sell>> GetDigisellerSellsAsync(int sellerId, List<int> productIds,
            DateTime dateStart, DateTime dateFinish, string sellerSecret)
        {
            string start = dateStart.ToString(GoogleDateTimeFormat);
            string end = dateFinish.ToString(GoogleDateTimeFormat);
            int page = 1;
            int totalPages;
            var sells = new List<SellsResult.Sell>();
            do
            {
                SellsResult dto = await Digiseller.GetSellsAsync(sellerId, productIds, start, end, page, sellerSecret);
                sells.AddRange(dto.Sells);
                ++page;
                totalPages = dto.Pages;
            } while (page <= totalPages);
            return sells;
        }

        private static async Task<string> GetTaxNameAsync(Transaction transaction, string taxNameFormat)
        {
            if (!transaction.DigisellerProductId.HasValue)
            {
                return transaction.Name;
            }

            ProductResult info = await Digiseller.GetProductsInfoAsync(transaction.DigisellerProductId.Value);
            string productName = info.Info.Name;
            return string.Format(taxNameFormat, productName);
        }

        private static async Task<Transaction> CreateTransactionAsync(SellsResult.Sell sell, string token)
        {
            string promoCode = await GetPromoCodeAsync(sell.InvoiceId, token);

            Transaction.PayMethod payMethod;
            switch (sell.PayMethodInfo)
            {
                case SellsResult.Sell.PayMethod.BankCard:
                    payMethod = Transaction.PayMethod.BankCard;
                    break;
                case SellsResult.Sell.PayMethod.Sbp:
                    payMethod = Transaction.PayMethod.Sbp;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return new Transaction(sell.ProductName, sell.DatePay, sell.AmountIn, sell.InvoiceId, sell.ProductId, payMethod,
                promoCode);
        }

        private static async Task<string> GetTokenAsync(string login, string password, string sellerSecret)
        {
            TokenResult result = await Digiseller.GetTokenAsync(login, password, sellerSecret);
            return result.Token;
        }

        private static async Task<string> GetPromoCodeAsync(int invoiceId, string token)
        {
            PurchaseResult result = await Digiseller.GetPurchaseAsync(invoiceId, token);
            return result.Info.PromoCode;
        }

        private const string GoogleDateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        #endregion // Digiseller

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region PayMaster

        public static async Task<List<Donation>> GetNewPayMasterPaymentsAsync(string siteAlias, DateTime start, DateTime end,
            string login, string password, IEnumerable<Donation> oldPayments)
        {
            List<ListPaymentsFilterResult.Response.Payment> allPayments =
                await GetPaymentsAsync(siteAlias, start, end, login, password);
            List<ListPaymentsFilterResult.Response.Payment> payments = allPayments.Where(p => !p.IsTestPayment).ToList();

            IEnumerable<int?> oldPaymentIds = oldPayments.Select(p => p.PaymentId);

            IEnumerable<ListPaymentsFilterResult.Response.Payment> newPayments =
                payments.Where(p => !oldPaymentIds.Contains(p.PaymentId));

            return newPayments.Select(p => new Donation(p)).ToList();
        }

        internal static string GetPayMasterHyperlink(int? paymentId) => GetHyperlink(PayMasterPaymentUrlFormat, paymentId);

        public static async Task<List<ListPaymentsFilterResult.Response.Payment>> GetPaymentsAsync(string siteAlias,
            DateTime start, DateTime end, string login, string password)
        {
            var result = new List<ListPaymentsFilterResult.Response.Payment>();
            DateTime periodFrom = start;
            while (periodFrom < end)
            {
                DateTime periodTo = Min(periodFrom + PayMasterMaxRequestPeriod, end);

                List<ListPaymentsFilterResult.Response.Payment> payments =
                    await GetPaymentsLimitedAsync(siteAlias, periodFrom, periodTo, login, password);
                result.AddRange(payments);

                periodFrom = periodTo.AddDays(1);
            }

            return result;
        }

        private static async Task<List<ListPaymentsFilterResult.Response.Payment>> GetPaymentsLimitedAsync(string siteAlias,
            DateTime periodFrom, DateTime periodTo, string login, string password)
        {
            string start = periodFrom.ToString(PayMasterDateTimeFormat);
            string end = periodTo.ToString(PayMasterDateTimeFormat);

            ListPaymentsFilterResult result =
                await PayMaster.GetPaymentsAsync(login, password, "", siteAlias, start, end, "", PayMasterState);

            return result.ResponseInfo.Payments;
        }

        public static void FindPayment(Transaction transaction,
            IEnumerable<ListPaymentsFilterResult.Response.Payment> payments, IEnumerable<string> purposesFormats)
        {
            if (!transaction.DigisellerSellId.HasValue || transaction.PayMasterPaymentId.HasValue)
            {
                return;
            }

            IEnumerable<string> purposes =
                purposesFormats.Select(f => string.Format(f, transaction.DigisellerSellId.Value));

            ListPaymentsFilterResult.Response.Payment payment =
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
                    payMasterFee = Round(donation.Amount * payMasterFeePercents[donation.PayMethodInfo.Value]);
                }
                donation.Total = donation.Amount - payMasterFee;

                donation.Week = (ushort) Math.Ceiling((donation.Date - firstThursday).TotalDays / 7);
            }
        }

        public static void CalculateShares(IEnumerable<Transaction> transactions, decimal taxFeePercent,
            decimal digisellerFeePercent, Dictionary<Transaction.PayMethod, decimal> payMasterFeePercents,
            Dictionary<string, List<Share>> shares)
        {
            var totals = new Dictionary<string, decimal>();
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
                        if (!transaction.PayMethodInfo.HasValue)
                        {
                            throw new ArgumentNullException();
                        }
                        decimal payMasterFee = Round(price * payMasterFeePercents[transaction.PayMethodInfo.Value]);
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
            string paramter = ExtractParameter(value, format);
            return int.TryParse(paramter, out int result) ? (int?) result : null;
        }

        public static string ExtractParameter(string value, string format)
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

        public static DateTime GetNextThursday(DateTime date)
        {
            int diff = (7 + DayOfWeek.Thursday - date.DayOfWeek) % 7;
            return date.AddDays(diff);
        }

        internal static string GetHyperlink(string urlFormat, object parameter)
        {
            string caption = parameter?.ToString();
            if (string.IsNullOrWhiteSpace(caption))
            {
                return null;
            }
            string url = string.Format(urlFormat, caption);
            var uri = new Uri(url);
            return GetHyperlink(uri, caption);
        }

        internal static string GetHyperlink(Uri uri, string caption)
        {
            return string.Format(HyperlinkFormat, uri.AbsoluteUri, caption);
        }

        private static decimal Round(decimal d) => Math.Round(d, 2);

        private const string HyperlinkFormat = "=HYPERLINK(\"{0}\";\"{1}\")";
        private const string NoProductSharesKey = "None";

        public static string PayMasterPaymentUrlFormat;

        private static readonly TimeSpan PayMasterMaxRequestPeriod = TimeSpan.FromDays(179);

        #endregion // Common
    }
}
