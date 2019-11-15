using System;
using System.Collections.Generic;
using BridgeBotNext.Configuration;
using BridgeBotNext.Entities;
using BridgeBotNext.Providers.Tg;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BridgeBotNextTest.Telegram
{
    public class TestTgProvider : TgProvider
    {
        public TestTgProvider(ILogger<TgProvider> logger, IOptions<TgConfiguration> configuration) : base(logger,
            configuration)
        {
        }

        public void SendTestButtons(Conversation conversation)
        {
            var chat = new ChatId(Convert.ToInt64(conversation.OriginId));
            IEnumerable<KeyboardButton> keys = new[]
            {
                new KeyboardButton("/pass"),
                new KeyboardButton("/fail")
            };
            BotClient.SendTextMessageAsync(chat, "🔹 /pass or /fail 🔹",
                replyMarkup: new ReplyKeyboardMarkup(keys, true, true));
        }
    }
}