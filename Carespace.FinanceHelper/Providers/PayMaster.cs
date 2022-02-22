using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Carespace.FinanceHelper.Data.PayMaster;
using GryphonUtilities;

namespace Carespace.FinanceHelper.Providers;

internal static class PayMaster
{
    public static Task<ListPaymentsFilterResult> GetPaymentsAsync(string login, string password, string accountId,
        string siteAlias, string periodFrom, string periodTo, string invoiceId, string state)
    {
        string nounce = GenerateNounce();
        string hash =
            Hash($"{login};{password};{nounce};{accountId};{siteAlias};{periodFrom};{periodTo};{invoiceId};{state}");

        Dictionary<string, string?> queryParameters = new()
        {
            ["login"] = login,
            ["nonce"] = nounce,
            ["hash"] = hash,
            ["siteAlias"] = siteAlias,
            ["periodFrom"] = periodFrom,
            ["periodTo"] = periodTo,
            ["state"] = state
        };

        return RestHelper.CallGetMethodAsync<ListPaymentsFilterResult>(ApiProvider, GetPaymentsMethod,
            queryParameters: queryParameters);
    }

    private static string GenerateNounce()
    {
        byte[] buffer = new byte[NounceSize];
        using (RandomNumberGenerator random = RandomNumberGenerator.Create())
        {
            random.GetBytes(buffer);
        }
        return Convert.ToBase64String(buffer);
    }

    private static string Hash(string input)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(input);
        using (SHA1 sha1 = SHA1.Create())
        {
            byte[] hash = sha1.ComputeHash(utf8);
            return Convert.ToBase64String(hash);
        }
    }

    private const string ApiProvider = "https://paymaster.ru";
    private const string GetPaymentsMethod = "api/v1/listPaymentsFilter";
    private const byte NounceSize = 20;
}