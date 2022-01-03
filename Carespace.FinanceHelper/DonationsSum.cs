using System;
using System.Collections.Generic;
using GoogleSheetsManager;

namespace Carespace.FinanceHelper
{
    public sealed class DonationsSum : ISavable
    {
        IList<string> ISavable.Titles => Titles;

        public readonly DateTime Date;

        private readonly decimal _amount;

        public DonationsSum(DateTime firstDate, ushort weeksPast, decimal amount)
            : this(firstDate.AddDays(weeksPast * 7), amount)
        {
        }

        private DonationsSum(DateTime date, decimal amount)
        {
            Date = date;
            _amount = amount;
        }

        public IDictionary<string, object> Save()
        {
            return new Dictionary<string, object>
            {
                { DateTitle, $"{Date:d MMMM yyyy}" },
                { AmountTitle, _amount }
            };
        }

        private static readonly List<string> Titles = new List<string>
        {
            DateTitle,
            AmountTitle
        };

        private const string DateTitle = "Дата";
        private const string AmountTitle = "Сумма";
    }
}
