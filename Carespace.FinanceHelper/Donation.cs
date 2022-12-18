﻿using System;
using System.Text.RegularExpressions;
using Carespace.FinanceHelper.Data.PayMaster;
using GoogleSheetsManager;
using GryphonUtilities.Extensions;
using JetBrains.Annotations;

namespace Carespace.FinanceHelper;

public sealed class Donation
{
    [SheetField("Дата", "{0:d MMMM yyyy}")]
    public DateOnly Date;

    [UsedImplicitly]
    [SheetField("Сумма")]
    public decimal Amount;

    [UsedImplicitly]
    [SheetField("Поступление")]
    public string? PaymentIdLink
    {
        get => PayMaster.Manager.GetHyperlink(PaymentId);
        set => PaymentId = value;
    }

    [SheetField("Способ")]
    [UsedImplicitly]
    public Transaction.PayMethod? PayMethodInfo;

    [UsedImplicitly]
    [SheetField("От кого")]
    public string? Name;

    [SheetField("Неделя")]
    public ushort Week;

    [SheetField("Итого")]
    public decimal Total;

    internal string? PaymentId;

    public Donation() { }

    internal Donation(PaymentsResult.Item payment)
    {
        Date = payment.Created.GetValue(nameof(payment.Created)).DateOnly;
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

        Name = payment.Invoice?.OrderNo;
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

    private const int CardNumberLength = 16;
    private const string CardNumberPart = "XXXXXX";
    private const string PhonePattern = ".*, 7\\d{10}$";
    private static readonly Regex PhoneRegex = new(PhonePattern);
}