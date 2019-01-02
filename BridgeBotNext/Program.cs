using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BridgeBotNext.Configuration;
using BridgeBotNext.Providers;
using BridgeBotNext.Providers.Tg;
using BridgeBotNext.Providers.Vk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BridgeBotNext
{
    internal class Program
    {
        private static void ConfigureServices(ServiceCollection services)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables("BOT_")
                .Build();

            services
                .AddLogging(configure => configure.AddConsole())
                .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Trace);

            services.Configure<TgConfiguration>(config.GetSection("Tg"));
            services.AddSingleton<TgProvider>();

            services.Configure<VkConfiguration>(config.GetSection("Vk"));

            services.AddSingleton<VkProvider>();
        }

        private static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var logger = serviceProvider.GetService<ILogger<Program>>();

            var providers = new List<Provider>();
            providers.Add(serviceProvider.GetService<TgProvider>());
            providers.Add(serviceProvider.GetService<VkProvider>());


            // End of providers
            if (!providers.Any())
            {
                logger.LogError("No providers enabled. Please provide bot tokens, if you wish enable bot provider");
                logger.LogError("Use env variables: TELEGRAM_BOT_TOKEN");

                Environment.Exit(1);
            }

            logger.LogInformation("Running bot with providers: {0}",
                string.Join(" ", providers.Select(prov => prov.Name)));

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

            logger.LogInformation("Bot is successfully started");

            #region Temporary

            providers.ForEach(provider => { provider.MessageReceived += OnMessageReceived; });

            #endregion

            ConsoleHost.WaitForShutdown();

            var disconnectionTask = Task.WhenAll(providers.Select(prov => prov.Disconnect()));
            logger.LogInformation("Graceful shutdown");
            done = disconnectionTask.Wait(15000);
            if (!done)
            {
                logger.LogError("Disconnection is timed out");
                Environment.Exit(1460);
            }
        }

        private static void OnMessageReceived(object sender, Provider.MessageEventArgs e)
        {
            var msg = e.Message;
            e.Message.OriginConversation.Provider.SendMessage(msg.OriginConversation, msg);
            Console.WriteLine($"{msg.OriginSender.DisplayName} ({msg.OriginConversation.Id}): {msg.Body}");
        }
    }
}