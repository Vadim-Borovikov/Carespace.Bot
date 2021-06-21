using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Carespace.Bot.Dto;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Carespace.Bot
{
    internal static class DigisellerProvider
    {
        public static SellsResult GetSells(int sellerId, List<int> productIds, string start, string end,
            int page, string sellerSecret)
        {
            string sign = Hash($"{sellerId}{string.Join("", productIds)}{start}{end}{Returned}{page}{RowsPerPage}{sellerSecret}");
            var dto = new SellsRequest
            {
                SellerId = sellerId,
                ProductIds = productIds,
                DateStart = start,
                DateFinish = end,
                Returned = Returned,
                Rows = RowsPerPage,
                Page = page,
                Sign = sign
            };

            return RestHelper.CallPostMethod<SellsResult>(ApiProvider, Method, dto, Settings);
        }
        private static string Hash(string input)
        {
            byte[] utf8 = Encoding.UTF8.GetBytes(input);
            var sb = new StringBuilder();
            using (var sha256 = new SHA256Managed())
            {
                byte[] hash = sha256.ComputeHash(utf8);
                foreach (byte b in hash)
                {
                    sb.Append($"{b:x2}");
                }
            }
            return sb.ToString();
        }

        private static readonly DefaultContractResolver ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        };
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = ContractResolver
        };

        private const string ApiProvider = "https://api.digiseller.ru/";
        private const string Method = "api/seller-sells";

        private const int RowsPerPage = 2000;
        private const int Returned = 1;
    }
}
