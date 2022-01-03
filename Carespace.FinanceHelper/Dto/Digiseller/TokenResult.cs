using Newtonsoft.Json;

namespace Carespace.FinanceHelper.Dto.Digiseller
{
    public sealed class TokenResult
    {
        [JsonProperty]
        public string Token { get; set; }
    }
}
