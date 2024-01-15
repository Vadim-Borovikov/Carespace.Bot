using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using GoogleSheetsManager;
using GoogleSheetsManager.Extensions;
using JetBrains.Annotations;

namespace Carespace.FinanceHelper;

public sealed class Transaction
{
    public readonly Dictionary<string, decimal> Shares = new();

    // Common URL formats
    public static string DigisellerProductUrlFormat = "";
    private const string EmailFormat = "mailto:{0}";

    [Required]
    [SheetField("Дата", "{0:d MMMM yyyy}")]
    public DateOnly Date;

    [UsedImplicitly]
    [SheetField("Комментарий")]
    public string? Name;

    [UsedImplicitly]
    [Required]
    [SheetField("Сумма")]
    public decimal Amount;

    [UsedImplicitly]
    [SheetField("Промокод")]
    public string? PromoCode;

    [UsedImplicitly]
    [SheetField("Товар")]
    public string? DigisellerProductIdLink
    {
        get => Hyperlink.From(DigisellerProductUrlFormat, DigisellerProductId);
        set => DigisellerProductId = value.ToInt();
    }

    [UsedImplicitly]
    [SheetField("Email")]
    public string? EmailLink
    {
        get => Hyperlink.From(EmailFormat, Email?.Address);
        set => Email = value.ToEmail();
    }

    public int? DigisellerProductId;

    public MailAddress? Email;

    public static void Save(Transaction t, IDictionary<string, object?> valueSet)
    {
        foreach (string agent in t.Shares.Keys)
        {
            valueSet[agent] = t.Shares[agent];
        }
    }
}