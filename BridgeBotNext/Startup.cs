using System;
using System.IO;
using BridgeBotNext.Configuration;
using BridgeBotNext.Providers.Tg;
using BridgeBotNext.Providers.Vk;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BridgeBotNext
{
    public class Startup: IStartup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.Use(async (ctx, next) =>
            {
                await ctx.Response.WriteAsync("OK");
            });
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, false)
                .AddEnvironmentVariables("BOT_")
                .Build();

            services
                .AddLogging(configure => configure.AddConsole())
                .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Trace);

            services.AddSingleton<BotOrchestrator>();

            services.Configure<TgConfiguration>(config.GetSection("Tg"));
            services.AddSingleton<TgProvider>();

            services.Configure<VkConfiguration>(config.GetSection("Vk"));
            services.AddSingleton<VkProvider>();

            services.Configure<AuthConfiguration>(config.GetSection("Auth"));

            var connectionString = config.GetValue("database", "bridgebot.db");
            services.AddSingleton(new LiteDatabase(connectionString));

            return services.BuildServiceProvider();
        }
    }
}