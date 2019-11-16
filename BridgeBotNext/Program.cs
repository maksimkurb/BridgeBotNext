﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BridgeBotNext.Entities;
using BridgeBotNext.Providers;
using BridgeBotNext.Providers.Tg;
using BridgeBotNext.Providers.Vk;
using LiteDB;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
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

            var providers = new List<Provider>();

            #region Registering providers

            providers.Add(serviceProvider.GetService<TgProvider>());
            providers.Add(serviceProvider.GetService<VkProvider>());

            #endregion

            if (!providers.Any())
            {
                logger.LogError("No providers enabled. Please provide bot tokens, if you wish enable bot provider");
                logger.LogError("Use env variables: TELEGRAM_BOT_TOKEN");

                Environment.Exit(1);
            }

            logger.LogInformation("Running bot with providers: {0}",
                string.Join(", ", providers.Select(prov => prov.Name)));

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

            BsonMapper.Global.RegisterType(
                provider => new BsonValue(provider.Name),
                providerName =>
                {
                    foreach (var provider in providers)
                        if (provider.Name == providerName.AsString)
                            return provider;

                    return null;
                });
            BsonMapper.Global.RegisterType(
                conversationId => new BsonValue(conversationId.ToString()),
                value =>
                {
                    var parts = value.AsString.Split(':', 2);
                    if (parts.Length != 2) return null;
                    foreach (var provider in providers)
                        if (provider.Name == parts[0])
                            return new ProviderId(provider, parts[1]);

                    return null;
                });

            var orchestrator = serviceProvider.GetService<BotOrchestrator>();

            foreach (var provider in providers) orchestrator.AddProvider(provider);

            logger.LogInformation("Bot is successfully started");

            #region Run app

            var herokuAppName = Environment.GetEnvironmentVariable("HEROKU_APP");
            if (!herokuAppName.IsNullOrEmpty())
            {
                var herokuWakeUp = new HerokuWakeUp(
                    serviceProvider.GetService<ILogger<HerokuWakeUp>>(),
                    new Uri($"http://{herokuAppName}.herokuapp.com"),
                    new TimeSpan(0, 5, 0),
                    CancellationToken.None);
                Task.Run(herokuWakeUp.PeriodicPing);
            }

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