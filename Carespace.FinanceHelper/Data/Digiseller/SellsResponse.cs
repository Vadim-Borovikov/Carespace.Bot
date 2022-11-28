using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using GryphonUtilities;
using JetBrains.Annotations;

namespace Carespace.FinanceHelper.Data.Digiseller;

public sealed class SellsResponse
{
    public sealed class Sell
    {
        [UsedImplicitly]
        [JsonConverter(typeof(JsonStringEnumMemberConverter))]
        public enum PayMethod
        {
            [EnumMember(Value = "Bank Card")]
            BankCard,
            [EnumMember(Value = "Faster Payments System")]
            Sbp
        }

        [UsedImplicitly]
        public int? InvoiceId;

        [UsedImplicitly]
        public int? ProductId;

        [UsedImplicitly]
        public string? ProductName;

        [UsedImplicitly]
        public DateTimeFull? DatePay;

        [UsedImplicitly]
        public decimal? AmountIn;

        [UsedImplicitly]
        [JsonPropertyName("method_pay")]
        public PayMethod? PayMethodInfo;

        [UsedImplicitly]
        public string? Email;
    }

    [UsedImplicitly]
    public int? Pages;

    [UsedImplicitly]
    [JsonPropertyName("rows")]
    public List<Sell?>? Sells;
}