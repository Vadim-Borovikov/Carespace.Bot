using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Carespace.FinanceHelper.Data.Digiseller;
using GryphonUtilities;

namespace Carespace.FinanceHelper.Providers;

internal static class Digiseller
{
    public static Task<SellsResponse> GetSellsAsync(int sellerId, List<int> productIds, string start, string end,
        int page, string sellerSecret, JsonSerializerOptions options)
    {
        string sign =
            Hash($"{sellerId}{string.Join("", productIds)}{start}{end}{Returned}{page}{RowsPerPage}{sellerSecret}");
        SellsRequest obj = new()
        {
            SellerId = sellerId,
            ProductIds = productIds.Select(i => (int?) i).ToList(),
            DateStart = start,
            DateFinish = end,
            Returned = Returned,
            Rows = RowsPerPage,
            Page = page,
            Sign = sign
        };

        return RestHelper.CallPostMethodAsync<SellsRequest, SellsResponse>(ApiProvider, GetSellsMethod, obj: obj,
            options: options);
    }

    public static Task<TokenResponse> GetTokenAsync(string login, string password, string sellerSecret,
        DateTimeFull now, JsonSerializerOptions options)
    {
        long timestamp = now.DateTimeOffset.ToFileTime();
        string sign = Hash($"{password}{sellerSecret}{timestamp}");
        TokenRequest obj = new()
        {
            Login = login,
            Timestamp = timestamp,
            Sign = sign
        };

        return RestHelper.CallPostMethodAsync<TokenRequest, TokenResponse>(ApiProvider, GetTokenMethod, obj: obj,
            options: options);
    }

    public static Task<PurchaseResponse> GetPurchaseAsync(int invoiceId, string token, JsonSerializerOptions options)
    {
        Dictionary<string, string?> queryParameters = new() { ["token"] = token };

        return RestHelper.CallGetMethodAsync<PurchaseResponse>(ApiProvider, $"{GetPurchaseMethod}{invoiceId}",
            queryParameters: queryParameters, options: options);
    }

    private static string Hash(string input)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(input);
        StringBuilder sb = new();
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(utf8);
            foreach (byte b in hash)
            {
                sb.Append($"{b:x2}");
            }
        }
        return sb.ToString();
    }

    private const string ApiProvider = "https://api.digiseller.ru/";
    private const string GetSellsMethod = "api/seller-sells";
    private const string GetTokenMethod = "api/apilogin";
    private const string GetPurchaseMethod = "api/purchase/info/";

    private const int RowsPerPage = 2000;
    private const int Returned = 1;
}