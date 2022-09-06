using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Carespace.FinanceHelper.Data.PayMaster;

public sealed class PaymentsResult
{
    public sealed class Item
    {
        public sealed class AmountInfo
        {
            [JsonProperty]
            public decimal? Value { get; set; }
        }

        public sealed class Payment
        {
            [JsonProperty]
            public string? PaymentMethod { get; set; }

            [JsonProperty]
            public string? PaymentInstrumentTitle { get; set; }
        }

        public sealed class InvoiceInfo
        {
            [JsonProperty]
            public string? Description { get; set; }

            [JsonProperty]
            public string? OrderNo { get; set; }
        }

        [JsonProperty]
        public int? Id { get; set; }

        [JsonProperty]
        public DateTime? Created { get; set; }

        [JsonProperty]
        public Payment? PaymentData { get; set; }

        [JsonProperty]
        public AmountInfo? Amount { get; set; }

        [JsonProperty]
        public InvoiceInfo? Invoice { get; set; }

        [JsonProperty]
        public string? Status { get; set; }

        [JsonProperty]
        public bool? TestMode { get; set; }
    }

    [JsonProperty]
    public List<Item?>? Items { get; set; }
}