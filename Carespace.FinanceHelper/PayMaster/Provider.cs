using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Carespace.FinanceHelper.Data.PayMaster;
using GryphonUtilities;

namespace Carespace.FinanceHelper.PayMaster;

internal static class Provider
{
    public static Task<PaymentsResult> GetPaymentsAsync(string token, string merchantId, string start, string end,
        JsonSerializerOptions options)
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

        return RestManager<PaymentsResult>.GetAsync(ApiProvider, GetPaymentsMethod, headerParameters, queryParameters,
            options);
    }

    private const string ApiProvider = "https://paymaster.ru";
    private const string GetPaymentsMethod = "api/v2/payments";
}