using Newtonsoft.Json;

namespace Carespace.FinanceHelper.Data.Digiseller;

public sealed class TokenResponse
{
    [JsonProperty]
    public string? Token { get; set; }
}