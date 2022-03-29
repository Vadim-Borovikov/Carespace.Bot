using System;
using System.Collections.Generic;
using System.Net.Mail;
using GoogleSheetsManager;
using GryphonUtilities;

namespace Carespace.FinanceHelper;

public sealed class Transaction : ISavable
{
    IList<string> ISavable.Titles => Titles;

    public enum PayMethod
    {
        BankCard,
        Sbp
    }

    public static readonly List<string> Agents = new();

    // Common URL formats
    public static string DigisellerSellUrlFormat = "";
    public static string DigisellerProductUrlFormat = "";
    private const string EmailFormat = "mailto:{0}";

    public static long TaxPayerId;

    public readonly DateTime Date;
    public readonly Dictionary<string, decimal> Shares = new();

    public decimal? DigisellerFee { get; internal set; }
    public decimal? PayMasterFee { get; internal set; }
    public decimal? Tax { get; internal set; }

    private readonly string? _taxReceiptId;
    internal int? PayMasterPaymentId;

    private readonly string? _name;
    internal readonly decimal Amount;
    internal readonly decimal? Price;
    internal readonly string? PromoCode;
    internal readonly int? DigisellerSellId;
    public readonly int? DigisellerProductId;
    public readonly MailAddress? Email;
    internal readonly PayMethod? PayMethodInfo;

    public bool NeedPaynemt => DigisellerSellId.HasValue && PayMasterPaymentId is null;

    internal Transaction(DateTime date, string? name, decimal price, string? promoCode, int? digisellerSellId,
        int? digisellerProductId, MailAddress? email, PayMethod? payMethod)
        : this(date, name, price, price, promoCode, digisellerSellId, digisellerProductId, payMethod, email)
    {
    }

    private Transaction(DateTime date, string? name, decimal amount, decimal? price, string? promoCode,
        int? digisellerSellId, int? digisellerProductId, PayMethod? payMethod, MailAddress? email = null,
        string? taxReceiptId = null, int? payMasterPaymentId = null)
    {
        Date = date;
        _name = name;
        Amount = amount;
        Price = price;
        PromoCode = promoCode;
        DigisellerSellId = digisellerSellId;
        DigisellerProductId = digisellerProductId;
        PayMethodInfo = payMethod;
        Email = email;
        _taxReceiptId = taxReceiptId;
        PayMasterPaymentId = payMasterPaymentId;
    }

    public static Transaction Load(IDictionary<string, object?> valueSet)
    {
        string? name = valueSet[NameTitle]?.ToString();

        DateTime date = valueSet[DateTitle].ToDateTime().GetValue($"Empty date in \"{name}\"");

        decimal amount = valueSet[AmountTitle].ToDecimal().GetValue($"Empty amount in \"{name}\"");

        decimal? price = valueSet[PriceTitle].ToDecimal();

        string? promoCode = valueSet[PromoCodeTitle]?.ToString();

        int? digisellerSellId =
            valueSet.ContainsKey(DigisellerSellIdTitle) ? valueSet[DigisellerSellIdTitle].ToInt() : null;

        int? digisellerProductId = valueSet[DigisellerProductIdTitle].ToInt();

        PayMethod? payMethod =
            valueSet.ContainsKey(PayMethodInfoTitle) ? valueSet[PayMethodInfoTitle].ToPayMathod() : null;

        MailAddress? email = valueSet.ContainsKey(EmailTitle) ? valueSet[EmailTitle].ToEmail() : null;

        string? taxReceiptId =
            valueSet.ContainsKey(TaxReceiptIdTitle) ? valueSet[TaxReceiptIdTitle]?.ToString() : null;

        int? payMasterPaymentId =
            valueSet.ContainsKey(PayMasterPaymentIdTitle) ? valueSet[PayMasterPaymentIdTitle].ToInt() : null;

        return new Transaction(date, name, amount, price, promoCode, digisellerSellId, digisellerProductId, payMethod,
            email, taxReceiptId, payMasterPaymentId);
    }

    public IDictionary<string, object?> Convert()
    {
        Uri? taxReceiptUri = string.IsNullOrWhiteSpace(_taxReceiptId)
            ? null
            : SelfWork.DataManager.GetReceiptUri(TaxPayerId, _taxReceiptId);
        string? taxHyperLink = taxReceiptUri is null ? null : Utils.GetHyperlink(taxReceiptUri, _taxReceiptId);
        Dictionary<string, object?> result = new()
        {
            { NameTitle, _name },
            { DateTitle, $"{Date:d MMMM yyyy}" },
            { AmountTitle, Amount },
            { PriceTitle, Price },
            { PromoCodeTitle, PromoCode },
            { DigisellerProductIdTitle, Utils.GetHyperlink(DigisellerProductUrlFormat, DigisellerProductId) },
            { EmailTitle, Utils.GetHyperlink(EmailFormat, Email?.Address) },
            { PayMethodInfoTitle, PayMethodInfo.ToString() },
            { DigisellerSellIdTitle, Utils.GetHyperlink(DigisellerSellUrlFormat, DigisellerSellId) },
            { PayMasterPaymentIdTitle, Utils.GetPayMasterHyperlink(PayMasterPaymentId) },
            { TaxReceiptIdTitle, taxHyperLink },
            { DigisellerFeeTitle, DigisellerFee },
            { PayMasterFeeTitle, PayMasterFee },
            { TaxTitle, Tax }
        };
        foreach (string agent in Agents)
        {
            result[agent] = Shares.ContainsKey(agent) ? Shares[agent] : null;
        }

        return result;
    }

    public static readonly List<string> Titles = new()
    {
        NameTitle,
        DateTitle,
        AmountTitle,
        PriceTitle,
        PromoCodeTitle,
        DigisellerProductIdTitle,
        EmailTitle,
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
    private const string EmailTitle = "Email";
    private const string PayMethodInfoTitle = "Способ";
    private const string DigisellerSellIdTitle = "Покупка";
    private const string PayMasterPaymentIdTitle = "Поступление";
    private const string TaxReceiptIdTitle = "Чек";
    private const string DigisellerFeeTitle = "Digiseller";
    private const string PayMasterFeeTitle = "Paymaster";
    private const string TaxTitle = "Налог";
}