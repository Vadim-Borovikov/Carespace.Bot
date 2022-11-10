using System;
using GoogleSheetsManager;
using JetBrains.Annotations;

namespace Carespace.FinanceHelper;

public sealed class DonationsSum
{
    [SheetField("Дата", "{0:d MMMM yyyy}")]
    public DateTime Date;

    [UsedImplicitly]
    [SheetField("Сумма")]
    public decimal Amount;

    public DonationsSum() { }

    public DonationsSum(DateTime firstDate, ushort weeksPast, decimal amount)
    {
        Date = firstDate.AddDays(weeksPast * 7);
        Amount = amount;
    }
}