using System.IO;
using Microsoft.Extensions.Configuration;

namespace Carespace.FinanceHelper.Tests
{
    internal static class Helper
    {
        public static Configuration GetConfig()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json") // Create appsettings.json for private settings
                .Build()
                .Get<Configuration>();
        }
    }
}
