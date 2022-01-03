using System;
using System.Collections.Generic;
using GoogleSheetsManager;

namespace Carespace.FinanceHelper
{
    public sealed class Transaction : ILoadable, ISavable
    {
        IList<string> ISavable.Titles => Titles;

        public enum PayMethod
        {
            BankCard,
            Sbp
        }

        // Common URL formats
        public static string DigisellerSellUrlFormat;
        public static string DigisellerProductUrlFormat;

        public static long TaxPayerId;

        public static List<string> Agents;

        // Data
        internal string Name { get; private set; }
        public DateTime Date { get; private set; }
        internal decimal Amount { get; private set; }
        internal decimal? Price { get; private set; }
        internal string PromoCode { get; private set; }
        internal int? DigisellerSellId { get; private set; }
        internal int? DigisellerProductId { get; private set; }
        internal string TaxReceiptId;
        internal int? PayMasterPaymentId;
        internal PayMethod? PayMethodInfo { get; private set; }
        public decimal? DigisellerFee;
        public decimal? PayMasterFee;
        public decimal? Tax;
        public readonly Dictionary<string, decimal> Shares = new Dictionary<string, decimal>();

        public bool NeedPaynemt => DigisellerSellId.HasValue && !PayMasterPaymentId.HasValue;

        public Transaction(decimal amount, decimal price, int digisellerProductId, string promoCode = null,
            int? digiSellerSellId = null, PayMethod? payMethodInfo = null)
        {
            Amount = amount;
            Price = price;
            DigisellerProductId = digisellerProductId;
            PromoCode = promoCode;
            DigisellerSellId = digiSellerSellId;
            PayMethodInfo = payMethodInfo;
        }

        public Transaction() { }

        internal Transaction(string productName, DateTime datePay, decimal price, int digisellerSellId,
            int digisellerProductId, PayMethod payMethod, string promoCode)
        {
            Name = productName;
            Date = datePay;
            Amount = price;
            Price = price;
            DigisellerSellId = digisellerSellId;
            DigisellerProductId = digisellerProductId;
            PayMethodInfo = payMethod;
            PromoCode = promoCode;
        }

        public void Load(IDictionary<string, object> valueSet)
        {
            Name = valueSet[NameTitle]?.ToString();

            Date = valueSet[DateTitle]?.ToDateTime() ?? throw new ArgumentNullException($"Empty date in \"{Name}\"");

            Amount = valueSet[AmountTitle]?.ToDecimal() ?? throw new ArgumentNullException($"Empty amount in \"{Name}\"");

            Price = valueSet[PriceTitle]?.ToDecimal();

            PromoCode = valueSet[PromoCodeTitle]?.ToString();

            DigisellerProductId = valueSet[DigisellerProductIdTitle]?.ToInt();

            PayMethodInfo = valueSet.ContainsKey(PayMethodInfoTitle) ? valueSet[PayMethodInfoTitle]?.ToPayMathod() : null;

            DigisellerSellId = valueSet.ContainsKey(DigisellerSellIdTitle) ? valueSet[DigisellerSellIdTitle]?.ToInt() : null;

            PayMasterPaymentId =
                valueSet.ContainsKey(PayMasterPaymentIdTitle) ? valueSet[PayMasterPaymentIdTitle]?.ToInt() : null;

            TaxReceiptId = valueSet.ContainsKey(TaxReceiptIdTitle) ? valueSet[TaxReceiptIdTitle]?.ToString() : null;
        }

        public IDictionary<string, object> Save()
        {
            Uri taxReceiptUri = SelfWork.DataManager.GetReceiptUri(TaxPayerId, TaxReceiptId);
            var result = new Dictionary<string, object>
            {
                { NameTitle, Name },
                { DateTitle, $"{Date:d MMMM yyyy}" },
                { AmountTitle, Amount },
                { PriceTitle, Price },
                { PromoCodeTitle, PromoCode },
                { DigisellerProductIdTitle, Utils.GetHyperlink(DigisellerProductUrlFormat, DigisellerProductId) },
                { PayMethodInfoTitle, PayMethodInfo.ToString() },
                { DigisellerSellIdTitle, Utils.GetHyperlink(DigisellerSellUrlFormat, DigisellerSellId) },
                { PayMasterPaymentIdTitle, Utils.GetPayMasterHyperlink(PayMasterPaymentId) },
                { TaxReceiptIdTitle, Utils.GetHyperlink(taxReceiptUri, TaxReceiptId) },
                { DigisellerFeeTitle, DigisellerFee },
                { PayMasterFeeTitle, PayMasterFee },
                { TaxTitle, Tax }
            };
            foreach (string agent in Agents)
            {
                result[agent] = Shares.ContainsKey(agent) ? Shares[agent] : (decimal?) null;
            }

            return result;
        }

        public static readonly List<string> Titles = new List<string>
        {
            NameTitle,
            DateTitle,
            AmountTitle,
            PriceTitle,
            PromoCodeTitle,
            DigisellerProductIdTitle,
            PayMethodInfoTitle,
            DigisellerSellIdTitle,
            PayMasterPaymentIdTitle,
            TaxReceiptIdTitle,
            DigisellerFeeTitle,
            PayMasterFeeTitle,
            TaxTitle
        };

        private const string NameTitle = "Комментарий";
        private const string DateTitle = "Дата";
        private const string AmountTitle = "Сумма";
        private const string PriceTitle = "Цена";
        private const string PromoCodeTitle = "Промокод";
        private const string DigisellerProductIdTitle = "Товар";
        private const string PayMethodInfoTitle = "Способ";
        private const string DigisellerSellIdTitle = "Покупка";
        private const string PayMasterPaymentIdTitle = "Поступление";
        private const string TaxReceiptIdTitle = "Чек";
        private const string DigisellerFeeTitle = "Digiseller";
        private const string PayMasterFeeTitle = "Paymaster";
        private const string TaxTitle = "Налог";
    }
}
