using System;
using System.Collections;
using System.Collections.Generic;
using BridgeBotNext;
using BridgeBotNext.Providers.Tg;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BridgeBotNextTest
{
    public class TestTelegramProvider: TelegramProvider
    {
        public TestTelegramProvider(string apiToken) : base(apiToken)
        {
        }

        public void SendTestButtons(Conversation conversation)
        {
            var chat = new ChatId(Convert.ToInt64(conversation.Id));
            IEnumerable<KeyboardButton> keys = new []
            {
                new KeyboardButton("/pass"),
                new KeyboardButton("/fail"),
            };
            BotClient.SendTextMessageAsync(chat, "🔹 /pass or /fail 🔹", replyMarkup: new ReplyKeyboardMarkup(keys, true, true));
        }
    }
}