using System;
using System.Globalization;
using AbstractBot;
using Carespace.Bot.Web.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Carespace.Bot.Web;

internal static class Program
{
    public static void Main(string[] args)
    {
        Utils.LogManager.DeleteExceptionLog();
        try
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            Models.Config config = Configure(builder);
            Utils.StartLogWith(config.SystemTimeZoneId);

            IServiceCollection services = builder.Services;
            services.AddControllersWithViews().AddNewtonsoftJson();

            AddBotTo(services);

            WebApplication app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors();

            UseUpdateEndpoint(app, config.Token);

            app.Run();
        }
        catch (Exception ex)
        {
            Utils.LogManager.LogException(ex);
        }
    }

    private static Models.Config Configure(WebApplicationBuilder builder)
    {
        ConfigurationManager configuration = builder.Configuration;
        Models.Config config = configuration.Get<Models.Config>();

        builder.Services.AddOptions<Models.Config>().Bind(configuration).ValidateDataAnnotations();
        builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<Models.Config>>().Value);

        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(config.CultureInfoName);

        return config;
    }

    private static void AddBotTo(IServiceCollection services)
    {
        services.AddSingleton<BotSingleton>();
        services.AddHostedService<BotService>();
    }

    private static void UseUpdateEndpoint(IApplicationBuilder app, string token)
    {
        object defaults = new
        {
            controller = "Update",
            action = "Post"
        };
        app.UseEndpoints(endpoints => endpoints.MapControllerRoute("update", token, defaults));
    }
}