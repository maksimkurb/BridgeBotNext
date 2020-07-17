using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BridgeBotNext.Entities;
using BridgeBotNext.Providers;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MixERP.Net.VCards.Extensions;

namespace BridgeBotNext
{
    internal class Program
    {
        public static string Version => "2.0.0";

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((hostContext, builder) =>
                {
                    builder.AddEnvironmentVariables("BOT_");

                    if (hostContext.HostingEnvironment.IsDevelopment())
                    {
                        builder.AddUserSecrets<Program>();
                    }
                })
                .UseKestrel(options =>
                {
                    options.ListenAnyIP(int.Parse(Environment.GetEnvironmentVariable("PORT").Or("8080")));
                });
        }

        private static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            var webHost = CreateWebHostBuilder(args).Build();
            var serviceProvider = webHost.Services;

            var logger = serviceProvider.GetService<ILogger<Program>>();

            var providers = serviceProvider.GetServices<Provider>().ToList();

            if (!providers.Any())
            {
                logger.LogError("No providers enabled. Please provide bot tokens, if you wish enable bot provider");
                logger.LogError("Use env variables: BOT_TG_BOTTOKEN, BOT_VK_ACCESSTOKEN, BOT_VK_GROUPID");

                Environment.Exit(1);
            }

            logger.LogInformation("Running bot with providers: {0}",
                string.Join(", ", providers.Select(prov => prov.Name)));

            var dbContext = serviceProvider.GetService<BotDbContext>();
            dbContext.Database.Migrate();
            
            #region Connect to providers

            var connectionTask = Task.WhenAll(providers.Select(prov => prov.Connect()));
            var done = connectionTask.Wait(60000);
            if (!done)
            {
                logger.LogError("Connection to some providers is timed out");
                Environment.Exit(1460);
            }

            if (connectionTask.IsFaulted)
            {
                logger.LogError("Connection to some providers is failed");
                Environment.Exit(59);
            }

            #endregion

            logger.LogTrace("Bot is successfully connected to all providers");


            var orchestrator = serviceProvider.GetService<BotOrchestrator>();

            foreach (var provider in providers) orchestrator.AddProvider(provider);

            logger.LogInformation("Bot is successfully started");

            #region Run app

            if (!Environment.GetEnvironmentVariable("PORT").IsNullOrEmpty())
                webHost.Run();
            else
                ConsoleHost.WaitForShutdown();

            logger.LogInformation("Graceful shutdown");
            foreach (var provider in providers) orchestrator.RemoveProvider(provider);

            providers.ForEach(prov => prov.Dispose());

            #endregion
        }
    }
}