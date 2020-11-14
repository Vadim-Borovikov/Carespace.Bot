using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMember.Global

namespace Carespace.Bot.Web
{
    internal sealed class Startup
    {
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<Models.Bot.IBot, Models.Bot.Bot>();
            services.AddHostedService<Models.Bot.Service>();
            services.Configure<Models.Bot.Configuration>(_configuration);

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("ru-RU");

            app.UseMvc(routes => routes.MapRoute("update", $"{_configuration["Token"]}/{{controller=Update}}/{{action=post}}"));
        }

        private readonly IConfiguration _configuration;
    }
}
