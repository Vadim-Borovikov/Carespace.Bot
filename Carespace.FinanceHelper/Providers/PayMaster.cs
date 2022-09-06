using System.Collections.Generic;
using System.Threading.Tasks;
using Carespace.FinanceHelper.Data.PayMaster;
using GryphonUtilities;

namespace Carespace.FinanceHelper.Providers;

internal static class PayMaster
{
    public static Task<PaymentsResult> GetPaymentsAsync(string token, string merchantId, string start, string end)
    {
        Dictionary<string, string> headerParameters = new()
        {
            ["Authorization"] = $"Bearer {token}"
        };

        Dictionary<string, string?> queryParameters = new()
        {
            ["merchantId"] = merchantId,
            ["start"] = start,
            ["end"] = end,
        };

        return RestHelper.CallGetMethodAsync<PaymentsResult>(ApiProvider, GetPaymentsMethod, headerParameters,
            queryParameters);
    }

    private const string ApiProvider = "https://paymaster.ru";
    private const string GetPaymentsMethod = "api/v2/payments";
}