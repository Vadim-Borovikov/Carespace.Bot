using System.Globalization;
using Carespace.Bot.Web.Models;
using GryphonUtilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Carespace.Bot.Web;

internal sealed class Startup
{
    public Startup(IConfiguration config) => _config = config;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<BotSingleton>();
        services.AddHostedService<BotService>();
        services.Configure<ConfigJson>(_config);

        services.AddControllersWithViews().AddNewtonsoftJson();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseCors();

        ConfigJson botConfig = _config.Get<ConfigJson>();

        string cultureInfoName = botConfig.CultureInfoName.GetValue(nameof(botConfig.Token));
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(cultureInfoName);

        string token = botConfig.Token.GetValue(nameof(botConfig.Token));
        object defaults = new
        {
            controller = "Update",
            action = "Post"
        };
        app.UseEndpoints(endpoints => endpoints.MapControllerRoute("update", token, defaults));
    }

    private readonly IConfiguration _config;
}