using System.Collections.Generic;
using Newtonsoft.Json;

namespace Carespace.FinanceHelper.Tests
{
    internal sealed class Configuration
    {
        [JsonProperty]
        public decimal TaxFeePercent { get; set; }

        [JsonProperty]
        public decimal DigisellerFeePercent { get; set; }

        [JsonProperty]
        public Dictionary<Transaction.PayMethod, decimal> PayMasterFeePercents { get; set; }

        [JsonProperty]
        public Dictionary<string, List<Share>> Shares { get; set; }
    }
}
