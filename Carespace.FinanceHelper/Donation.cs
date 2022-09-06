using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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


    internal Donation(PaymentsResult.Item payment)
    {
        Date = payment.Created.GetValue(nameof(payment.Created));
        Amount = (payment.Amount?.Value).GetValue(nameof(payment.Amount.Value));

        PaymentId = payment.Id;

        PaymentsResult.Item.Payment paymentData = payment.PaymentData.GetValue(nameof(payment.PaymentData));
        PayMethodInfo = paymentData.PaymentMethod switch
        {
            "sbp"      => Transaction.PayMethod.Sbp,
            "bankcard" => Transaction.PayMethod.BankCard,
            "qiwi"     => Transaction.PayMethod.BankCard,
            null       => Analyze(paymentData.PaymentInstrumentTitle),
            _ => throw new ArgumentOutOfRangeException(nameof(paymentData.PaymentMethod), paymentData.PaymentMethod,
                null)
        };

        _name = payment.Invoice?.OrderNo;
    }

    private static Transaction.PayMethod? Analyze(string? paymentDataTitle)
    {
        if (string.IsNullOrWhiteSpace(paymentDataTitle))
        {
            return null;
        }

        if (paymentDataTitle.Length is CardNumberLength && paymentDataTitle.Contains(CardNumberPart))
        {
            return Transaction.PayMethod.BankCard;
        }

        if (PhoneRegex.IsMatch(paymentDataTitle))
        {
            return Transaction.PayMethod.Sbp;
        }

        throw new ArgumentOutOfRangeException(nameof(paymentDataTitle), paymentDataTitle, "Can't analyze pay method");
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

    private const int CardNumberLength = 16;
    private const string CardNumberPart = "XXXXXX";
    private const string PhonePattern = ".*, 7\\d{10}$";
    private static readonly Regex PhoneRegex = new(PhonePattern);
}