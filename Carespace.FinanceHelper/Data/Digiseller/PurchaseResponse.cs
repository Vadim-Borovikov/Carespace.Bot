using JetBrains.Annotations;

namespace Carespace.FinanceHelper.Data.Digiseller;

internal sealed class PurchaseResponse
{
    public sealed class ContentInfo
    {
        [UsedImplicitly]
        public string? PromoCode;
    }

    [UsedImplicitly]
    public ContentInfo? Content;
}