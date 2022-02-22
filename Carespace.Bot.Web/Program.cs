using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Carespace.Bot.Web;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            await CreateWebHostBuilder(args).Build().RunAsync();
        }
        catch (Exception ex)
        {
            await AbstractBot.Utils.LogExceptionAsync(ex);
        }
    }

    private static IHostBuilder CreateWebHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
                   .ConfigureLogging((context, builder) =>
                   {
                       builder.AddConfiguration(context.Configuration.GetSection("Logging"));
                       builder.AddFile(o => o.RootPath = context.HostingEnvironment.ContentRootPath);
                   })
                   .ConfigureWebHostDefaults(builder => builder.UseStartup<Startup>());
    }
}
