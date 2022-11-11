using System;
using System.Collections.Generic;
using Carespace.FinanceHelper.Data.PayMaster;
using GoogleSheetsManager;
using GryphonUtilities;

namespace Carespace.FinanceHelper;

public sealed class Donation : ISavable
{
    IList<string> ISavable.Titles => Titles;

    public readonly DateTime Date;
    internal readonly decimal Amount;
    internal readonly int? PaymentId;
    internal readonly Transaction.PayMethod? PayMethodInfo;

    public ushort Week { get; internal set; }
    public decimal Total { get; internal set; }


    internal Donation(ListPaymentsFilterResult.ResponseInfo.Payment payment)
    {
        Date = payment.LastUpdateTime.GetValue(nameof(payment.LastUpdateTime));
        Amount = payment.PaymentAmount.GetValue(nameof(payment.PaymentAmount));

        PaymentId = payment.PaymentId;

        PayMethodInfo = payment.PaymentSystemId switch
        {
            161 => Transaction.PayMethod.Sbp,
            162 => Transaction.PayMethod.BankCard,
            _   => throw new ArgumentOutOfRangeException(nameof(payment.PaymentSystemId), payment.PaymentSystemId,
                null)
        };

        _name = payment.SiteInvoiceId.GetValue(nameof(payment.SiteInvoiceId));
    }

    private Donation(DateTime date, decimal amount, int? paymentId, Transaction.PayMethod? payMethodInfo, string? name)
    {
        Date = date;
        Amount = amount;
        PaymentId = paymentId;
        PayMethodInfo = payMethodInfo;
        _name = name;
    }

    public static Donation Load(IDictionary<string, object?> valueSet)
    {
        DateTime date = valueSet[DateTitle].ToDateTime().GetValue("Empty date");

        decimal amount = valueSet[AmountTitle].ToDecimal().GetValue("Empty amount");

        int? paymentId = valueSet.ContainsKey(PaymentIdTitle) ? valueSet[PaymentIdTitle]?.ToInt() : null;

        Transaction.PayMethod? payMethodInfo =
            valueSet.ContainsKey(PayMethodInfoTitle) ? valueSet[PayMethodInfoTitle]?.ToPayMathod() : null;

        string? name = valueSet[NameTitle]?.ToString();

        return new Donation(date, amount, paymentId, payMethodInfo, name);
    }

    public IDictionary<string, object?> Convert()
    {
        return new Dictionary<string, object?>
        {
            { NameTitle, _name ?? "" },
            { DateTitle, $"{Date:d MMMM yyyy}" },
            { AmountTitle, Amount },
            { PayMethodInfoTitle, PayMethodInfo?.ToString() ?? "" },
            { PaymentIdTitle, Utils.GetPayMasterHyperlink(PaymentId) },
            { TotalTitle, Total },
            { WeekTitle, Week }
        };
    }

    private static readonly List<string> Titles = new()
    {
        NameTitle,
        DateTitle,
        AmountTitle,
        PayMethodInfoTitle,
        PaymentIdTitle,
        TotalTitle,
        WeekTitle
    };

    private const string NameTitle = "От кого";
    private const string DateTitle = "Дата";
    private const string AmountTitle = "Сумма";
    private const string PayMethodInfoTitle = "Способ";
    private const string PaymentIdTitle = "Поступление";
    private const string TotalTitle = "Итого";
    private const string WeekTitle = "Неделя";

    private readonly string? _name;
}