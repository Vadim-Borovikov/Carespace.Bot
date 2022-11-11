using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Carespace.FinanceHelper.Data.PayMaster;

public sealed class ListPaymentsFilterResult
{
    public sealed class ResponseInfo
    {
        public sealed class Payment
        {
            [JsonProperty]
            public int? PaymentId { get; set; }

            [JsonProperty]
            public string? Purpose { get; set; }

            [JsonProperty]
            public int? PaymentSystemId { get; set; }

            [JsonProperty]
            public decimal? PaymentAmount { get; set; }

            [JsonProperty]
            public DateTime? LastUpdateTime { get; set; }

            [JsonProperty]
            public string? SiteInvoiceId { get; set; }

            [JsonProperty]
            public bool? IsTestPayment { get; set; }
        }

        [JsonProperty]
        public List<Payment?>? Payments { get; set; }
    }

    [JsonProperty]
    public ResponseInfo? Response { get; set; }
}