using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Carespace.FinanceHelper.Dto.Digiseller
{
    public sealed class SellsResult
    {
        public sealed class Sell
        {
            public enum PayMethod
            {
                [EnumMember(Value = "Bank Card")]
                [JsonProperty]
                BankCard,
                [EnumMember(Value = "Faster Payments System")]
                [JsonProperty]
                Sbp
            }

            [JsonProperty]
            public int InvoiceId { get; set; }
            [JsonProperty]
            public int ProductId { get; set; }
            [JsonProperty]
            public string ProductName { get; set; }
            [JsonProperty]
            public DateTime DatePay { get; set; }
            [JsonProperty]
            public decimal AmountIn { get; set; }
            [JsonProperty("method_pay")]
            public PayMethod PayMethodInfo { get; set; }
            [JsonProperty]
            public string Email { get; set; }
        }

        [JsonProperty]
        public int Pages { get; set; }
        [JsonProperty("rows")]
        public List<Sell> Sells { get; set; }
    }
}
