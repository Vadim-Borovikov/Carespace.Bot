using Newtonsoft.Json;
using RestSharp;

namespace Carespace.Bot
{
    internal static class RestHelper
    {
        public static TResult CallPostMethod<TResult>(string apiProvider, string method, object dto,
            JsonSerializerSettings settings, string token = null)
        {
            string json = JsonConvert.SerializeObject(dto, settings);

            IRestResponse response = CallPostMethod(apiProvider, method, json, token);

            return JsonConvert.DeserializeObject<TResult>(response.Content, settings);
        }

        private static IRestResponse CallPostMethod(string apiProvider, string method, string json, string token)
        {
            var client = new RestClient($"{apiProvider}{method}");
            var request = new RestRequest { Method = Method.POST };

            if (!string.IsNullOrEmpty(token))
            {
                request.AddHeader("Authorization", $"Bearer {token}");
            }
            request.AddParameter("application/json", json, ParameterType.RequestBody);

            return client.Execute(request);
        }
    }
}
