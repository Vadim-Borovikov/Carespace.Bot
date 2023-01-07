namespace Carespace.Bot.Email;

internal readonly struct RequestInfo
{
    public readonly string Key;
    public readonly string? Name;
    public readonly string? Promocode;
    public readonly decimal? Amount;

    private RequestInfo(string key, string? name = null)
    {
        Key = key;
        Name = name;
    }

    private RequestInfo(string key, string name, string promocode, decimal amount) : this(key, name)
    {
        Promocode = promocode;
        Amount = amount;
    }

    public static RequestInfo? Parse(string request)
    {
        string[] parts = request.Split();
        return parts.Length switch
        {
            1 => new RequestInfo(parts[0]),
            2 => new RequestInfo(parts[0], parts[1]),
            4 => decimal.TryParse(parts[3], out decimal amount)
                ? new RequestInfo(parts[0], parts[1], parts[2], amount)
                : null,
            _ => null
        };
    }
}