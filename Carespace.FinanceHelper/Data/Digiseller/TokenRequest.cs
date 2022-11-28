using JetBrains.Annotations;

namespace Carespace.FinanceHelper.Data.Digiseller;

internal sealed class TokenRequest
{
    [UsedImplicitly]
    public string? Login;

    [UsedImplicitly]
    public long? Timestamp;

    [UsedImplicitly]
    public string? Sign;
}