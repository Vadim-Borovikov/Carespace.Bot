using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Carespace.FinanceHelper.Dto.PayMaster
{
    public sealed class ListPaymentsFilterResult
    {
        public sealed class Response
        {
            public sealed class Payment
            {
                [JsonProperty]
                public int PaymentId { get; set; }

                [JsonProperty]
                public string Purpose { get; set; }

                [JsonProperty]
                public int PaymentSystemId { get; set; }

                [JsonProperty]
                public decimal PaymentAmount { get; set; }

                [JsonProperty]
                public DateTime LastUpdateTime { get; set; }

                [JsonProperty]
                public string SiteInvoiceId { get; set; }

                [JsonProperty]
                public bool IsTestPayment { get; set; }
            }

            [JsonProperty]
            public List<Payment> Payments { get; set; }
        }

        [JsonProperty("response")]
        public Response ResponseInfo { get; set; }
    }
}
