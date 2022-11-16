using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Carespace.FinanceHelper.Data.Digiseller;
using GryphonUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Carespace.FinanceHelper.Providers;

internal static class Digiseller
{
    public static Task<SellsResponse> GetSellsAsync(int sellerId, List<int> productIds, string start, string end,
        int page, string sellerSecret)
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
            settings: Settings);
    }

    public static Task<TokenResponse> GetTokenAsync(string login, string password, string sellerSecret)
    {
        long timestamp = DateTimeOffset.Now.ToFileTime();
        string sign = Hash($"{password}{sellerSecret}{timestamp}");
        TokenRequest obj = new()
        {
            Login = login,
            Timestamp = timestamp,
            Sign = sign
        };

        return RestHelper.CallPostMethodAsync<TokenRequest, TokenResponse>(ApiProvider, GetTokenMethod, obj: obj,
            settings: Settings);
    }

    public static Task<PurchaseResponse> GetPurchaseAsync(int invoiceId, string token)
    {
        Dictionary<string, string?> queryParameters = new() { ["token"] = token };

        return RestHelper.CallGetMethodAsync<PurchaseResponse>(ApiProvider, $"{GetPurchaseMethod}{invoiceId}",
            queryParameters: queryParameters);
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

    private static readonly JsonSerializerSettings Settings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        }
    };

    private const string ApiProvider = "https://api.digiseller.ru/";
    private const string GetSellsMethod = "api/seller-sells";
    private const string GetTokenMethod = "api/apilogin";
    private const string GetPurchaseMethod = "api/purchase/info/";

    private const int RowsPerPage = 2000;
    private const int Returned = 1;
}