using System;
using BridgeBotNext.Configuration;
using BridgeBotNext.Entities;
using BridgeBotNext.Providers;
using BridgeBotNext.Providers.Tg;
using BridgeBotNext.Providers.Vk;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BridgeBotNext
{
    public class Startup: IStartup
    {
        private readonly IConfiguration config;

        public Startup(IConfiguration config)
        {
            this.config = config;
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Use(async (ctx, next) =>
            {
                await ctx.Response.WriteAsync("OK");
            });
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services
                .AddLogging(configure => configure.AddConsole())
                .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Trace);

            services.AddSingleton<BotOrchestrator>();

            var tgConfig = config.GetSection("Tg");
            if (tgConfig.Exists()) {
                services.Configure<TgConfiguration>(tgConfig);
                services.AddSingleton<Provider, TgProvider>();
            }

            var vkConfig = config.GetSection("Vk");
            if (vkConfig.Exists()) {
                services.Configure<VkConfiguration>(vkConfig);
                services.AddSingleton<Provider, VkProvider>();
            }

            services.Configure<AuthConfiguration>(config.GetSection("Auth"));
            var dbBackend = config.GetValue("DbProvider", "sqlite").ToLower();
            switch (dbBackend)
            {
                // Check Provider and get ConnectionString
                case "sqlite":
                    services.AddDbContext<BotDbContext>(options =>
                        options.UseSqlite(config.GetConnectionString("sqlite")));
                    break;
                case "postgres":
                    services.AddDbContext<BotDbContext>(options =>
                        options.UseNpgsql(config.GetConnectionString("postgres")));
                    break;
                case "heroku-postgres":
                {
                    var connUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
                    connUrl = connUrl.Replace("postgres://", string.Empty);
                    var pgUserPass = connUrl.Split("@")[0];
                    var pgHostPortDb = connUrl.Split("@")[1];
                    var pgHostPort = pgHostPortDb.Split("/")[0];
                    var pgDb = pgHostPortDb.Split("/")[1];
                    var pgUser = pgUserPass.Split(":")[0];
                    var pgPass = pgUserPass.Split(":")[1];
                    var pgHost = pgHostPort.Split(":")[0];
                    var pgPort = pgHostPort.Split(":")[1];
                    var connStr = $"Server={pgHost};Port={pgPort};User Id={pgUser};Password={pgPass};Database={pgDb}";
                    services.AddDbContext<BotDbContext>(options => 
                        options.UseNpgsql(connStr)
                    );
                    break;
                }
                default:
                    throw new ArgumentException("Not a valid database type");
            }

            return services.BuildServiceProvider();
        }
    }
}