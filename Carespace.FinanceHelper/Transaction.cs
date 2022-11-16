using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using GoogleSheetsManager;
using JetBrains.Annotations;

namespace Carespace.FinanceHelper;

public sealed class Transaction
{
    public enum PayMethod
    {
        BankCard,
        Sbp
    }

    public readonly Dictionary<string, decimal> Shares = new();

    // Common URL formats
    public static string DigisellerSellUrlFormat = "";
    public static string DigisellerProductUrlFormat = "";
    private const string EmailFormat = "mailto:{0}";

    [Required]
    [SheetField("Дата", "{0:d MMMM yyyy}")]
    public DateOnly Date;

    [SheetField("Digiseller")]
    public decimal? DigisellerFee { get; internal set; }

    [SheetField("Paymaster")]
    public decimal? PayMasterFee { get; internal set; }

    [SheetField("Налог")]
    public decimal? Tax { get; internal set; }

    [UsedImplicitly]
    [SheetField("Чек")]
    public string? TaxReceiptIdLink
    {
        get
        {
            Uri? taxReceiptUri = string.IsNullOrWhiteSpace(_taxReceiptId)
                ? null
                : SelfWork.DataManager.GetReceiptUri(TaxPayerId, _taxReceiptId);
            return taxReceiptUri is null ? null : Utils.GetHyperlink(taxReceiptUri, _taxReceiptId);
        }
        set => _taxReceiptId = value;
    }

    [UsedImplicitly]
    [SheetField("Поступление")]
    public string? PayMasterPaymentIdLink
    {
        get => Utils.GetPayMasterHyperlink(PayMasterPaymentId);
        set => PayMasterPaymentId = value.ToInt();
    }

    [UsedImplicitly]
    [SheetField("Комментарий")]
    public string? Name;

    [UsedImplicitly]
    [Required]
    [SheetField("Сумма")]
    public decimal Amount;

    [UsedImplicitly]
    [SheetField("Цена")]
    public decimal? Price;

    [UsedImplicitly]
    [SheetField("Промокод")]
    public string? PromoCode;

    [UsedImplicitly]
    [SheetField("Покупка")]
    public string? DigisellerSellIdLink
    {
        get => Utils.GetHyperlink(DigisellerSellUrlFormat, DigisellerSellId);
        set => DigisellerSellId = value.ToInt();
    }

    [UsedImplicitly]
    [SheetField("Товар")]
    public string? DigisellerProductIdLink
    {
        get => Utils.GetHyperlink(DigisellerProductUrlFormat, DigisellerProductId);
        set => DigisellerProductId = value.ToInt();
    }

    [UsedImplicitly]
    [SheetField("Email")]
    public string? EmailLink
    {
        get => Utils.GetHyperlink(EmailFormat, Email?.Address);
        set => Email = value.ToEmail();
    }

    [UsedImplicitly]
    [SheetField("Способ", "{0}")]
    public PayMethod? PayMethodInfo;

    public static long TaxPayerId;

    public int? DigisellerProductId;

    public MailAddress? Email;

    public bool NeedPaynemt => DigisellerSellId.HasValue && PayMasterPaymentId is null;

    internal int? PayMasterPaymentId;

    internal int? DigisellerSellId;

    public Transaction() { }

    public static void Save(Transaction t, IDictionary<string, object?> valueSet)
    {
        foreach (string agent in t.Shares.Keys)
        {
            valueSet[agent] = t.Shares[agent];
        }
    }

    public Transaction(DateOnly date, string? name, decimal price, string? promoCode, int? digisellerSellId,
        int? digisellerProductId, MailAddress? email, PayMethod? payMethod)
    {
        Date = date;
        Name = name;
        Amount = price;
        Price = price;
        PromoCode = promoCode;
        DigisellerSellId = digisellerSellId;
        DigisellerProductId = digisellerProductId;
        PayMethodInfo = payMethod;
        Email = email;
    }

    private string? _taxReceiptId;
}