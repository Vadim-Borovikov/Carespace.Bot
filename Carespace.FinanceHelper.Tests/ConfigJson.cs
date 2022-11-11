using System.Collections.Generic;
using GoogleSheetsManager;
using GryphonUtilities;
using Newtonsoft.Json;

namespace Carespace.FinanceHelper.Tests;

internal sealed class ConfigJson
{
    public sealed class ShareJson : IConvertibleTo<Share>
    {
        [JsonProperty]
        public string? Agent { get; set; }
        [JsonProperty]
        public string? Promo { get; set; }
        [JsonProperty]
        public bool? PromoForNet { get; set; }
        [JsonProperty]
        public decimal? Limit { get; set; }
        [JsonProperty]
        public decimal Value { get; set; }
        [JsonProperty]
        public decimal? ValueAfterLimit { get; set; }

        public Share Convert()
        {
            string agent = Agent.GetValue(nameof(Agent));
            decimal value = Value.GetValue(nameof(Value));
            return new Share(agent, ValueAfterLimit, Promo, PromoForNet, Limit, value);
        }
    }

    [JsonProperty]
    public decimal? TaxFeePercent { get; set; }

    [JsonProperty]
    public decimal? DigisellerFeePercent { get; set; }

    [JsonProperty]
    public Dictionary<Transaction.PayMethod, decimal>? PayMasterFeePercents { get; set; }

    [JsonProperty]
    public Dictionary<string, List<ShareJson>>? ShareJsons { get; set; }
}