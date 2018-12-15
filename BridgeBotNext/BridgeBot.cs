using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BridgeBotNext.Providers;
using BridgeBotNext.Providers.Tg;

namespace BridgeBotNext
{
    internal class BridgeBot
    {
        private static void Main(string[] args)
        {
            var logger =
                Logging.LogService.GetLogger<BridgeBot>();

            var providers = new List<Provider>();

            // Telegram
            var telegramBotToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
            if (telegramBotToken != null)
                providers.Add(new TelegramProvider(telegramBotToken));

            // End of providers
            if (!providers.Any())
            {
                logger.Error("No providers enabled. Please provide bot tokens, if you wish enable bot provider");
                logger.Error("Use env variables: TELEGRAM_BOT_TOKEN");

                Environment.Exit(1);
            }

            logger.InfoFormat("Running bot with providers: {0}", string.Join(" ", providers.Select(prov => prov.Name)));

            var connectionTask = Task.WhenAll(providers.Select(prov => prov.Connect()));
            var done = connectionTask.Wait(60000);
            if (!done)
            {
                logger.Error("Connection to some providers is timed out");
                Environment.Exit(1460);
            }

            if (connectionTask.IsFaulted)
            {
                logger.Error("Connection to some providers is failed");
                Environment.Exit(59);
            }

            logger.Info("Bot is successfully started");

            #region Temporary

            providers.ForEach(provider => { provider.MessageReceived += OnMessageReceived; });

            #endregion

            ConsoleHost.WaitForShutdown();

            var disconnectionTask = Task.WhenAll(providers.Select(prov => prov.Disconnect()));
            logger.Info("Graceful shutdown");
            done = disconnectionTask.Wait(15000);
            if (!done)
            {
                logger.Error("Disconnection is timed out");
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