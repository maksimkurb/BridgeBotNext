using System;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Easy.Logger.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;

namespace BridgeBotNext.Providers
{
    /// <inheritdoc />
    public class TelegramProvider: Provider
    {
        private IEasyLogger _logger = Logging.LogService.GetLogger<TelegramProvider>();
        private TelegramBotClient _botClient { get; }
        public override string Name => "tg";
        public override string DisplayName => "Telegram";

        public TelegramProvider(string apiToken)
        {
            _botClient = new TelegramBotClient(apiToken);
            _botClient.OnMessage += _onMessage;
        }

        public override Task Connect()
        {
            return Task.Factory.StartNew(async () =>
            {
                var me = await _botClient.GetMeAsync();
                _logger.DebugFormat("Telegram bot @{0} ({1} {2}) connected", me.Username, me.FirstName, me.LastName);
                _botClient.StartReceiving();
            });
        }

        public override Task Disconnect()
        {
            return Task.Factory.StartNew(() =>
            {
                _logger.DebugFormat("Telegram bot stops receiving events");
                _botClient.StopReceiving();
            });
        }

        public override Task SendMessage(Conversation conversation, Message message)
        {
            return _botClient.SendTextMessageAsync(new ChatId(conversation.Id), message.Body);
        }

        private Conversation _extractConversation(Chat tgChat)
        {
            return new Conversation(this, tgChat.Id.ToString(), tgChat.Title);
        }
        
        private TelegramPerson _extractPerson(User tgUser)
        {
            var fullName = new StringBuilder().Append(tgUser.FirstName);
            if (tgUser.LastName != null)
                fullName.AppendFormat(" {0}", tgUser.LastName);
            return new TelegramPerson(this, tgUser.Username, fullName.ToString());
        }
        
        private Message _extractMessage(Telegram.Bot.Types.Message tgMessage)
        {
            Conversation conversation = _extractConversation(tgMessage.Chat);
            Person person = _extractPerson(tgMessage.From);
            return new Message(conversation, person, tgMessage.Text);
        }
        
        private void _onMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            OnMessageReceived(new MessageEventArgs(_extractMessage(e.Message)));
        }

    }
}