using System.Collections.Generic;
using GryphonUtilities.Time;
using JetBrains.Annotations;

namespace Carespace.FinanceHelper.Data.PayMaster;

public sealed class PaymentsResult
{
    public sealed class Item
    {
        public sealed class AmountInfo
        {
            [UsedImplicitly]
            public decimal? Value;
        }

        public sealed class Payment
        {
            [UsedImplicitly]
            public string? PaymentMethod;

            [UsedImplicitly]
            public string? PaymentInstrumentTitle;
        }

        public sealed class InvoiceInfo
        {
            [UsedImplicitly]
            public string? Description;

            [UsedImplicitly]
            public string? OrderNo;
        }

        [UsedImplicitly]
        public string? Id;

        [UsedImplicitly]
        public DateTimeFull? Created;

        [UsedImplicitly]
        public Payment? PaymentData;

        [UsedImplicitly]
        public AmountInfo? Amount;

        [UsedImplicitly]
        public InvoiceInfo? Invoice;

        [UsedImplicitly]
        public string? Status;

        [UsedImplicitly]
        public bool? TestMode;
    }

    [UsedImplicitly]
    public List<Item?>? Items;
}