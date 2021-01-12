using System.Diagnostics.CodeAnalysis;
using Carespace.Bot.Web.Models;
using Carespace.Bot.Web.Models.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Carespace.Bot.Web
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    internal sealed class Startup
    {
        public Startup(IConfiguration config) => _config = config;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IBot, Models.Bot>();
            services.AddHostedService<Service>();
            services.Configure<Config>(_config);

            services.AddMvc();
        }

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

            app.UseMvc(routes => routes.MapRoute("update", $"{_config["Token"]}/{{controller=Update}}/{{action=post}}"));
        }

        private readonly IConfiguration _config;
    }
}
