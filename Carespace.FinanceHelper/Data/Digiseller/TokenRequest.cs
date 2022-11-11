﻿using Newtonsoft.Json;

namespace Carespace.FinanceHelper.Data.Digiseller;

public sealed class TokenRequest
{
    [JsonProperty]
    public string? Login { get; set; }

    [JsonProperty]
    public long? Timestamp { get; set; }

    [JsonProperty]
    public string? Sign { get; set; }
}