using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BridgeBotNext.Entities;
using BridgeBotNext.Providers;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace BridgeBotNext
{
    public class BotOrchestrator
    {
        private static readonly char[] CommandArgsSplitters = {' ', '_'};
        private static readonly string BotPrefix = "🔹 ";
        private static readonly string CurrentChatPrefix = "📍";
        private readonly LiteCollection<Connection> _connections;
        private readonly LiteCollection<Conversation> _conversations;
        private LiteDatabase _db;
        private readonly ILogger<BotOrchestrator> _logger;
        private readonly List<Provider> _providers = new List<Provider>();

        public BotOrchestrator(ILogger<BotOrchestrator> logger, LiteDatabase db)
        {
            _logger = logger;
            _db = db;
            _connections = db.GetCollection<Connection>("connections");
            _conversations = db.GetCollection<Conversation>("conversations");
        }

        public void AddProvider(Provider provider)
        {
            _providers.Add(provider);
            provider.MessageReceived += OnMessageReceived;
            provider.CommandReceived += OnCommandReceived;
        }

        public void RemoveProvider(Provider provider)
        {
            if (_providers.Remove(provider))
            {
                provider.MessageReceived -= OnMessageReceived;
                provider.CommandReceived -= OnCommandReceived;
            }
        }

        private async void OnCommandReceived(object sender, Provider.MessageEventArgs e)
        {
            var conversation = e.Message.OriginConversation;

            var messageBody = e.Message.Body;
            _logger.LogTrace(
                $"Command received from {conversation.Provider.DisplayName}, conversationId: {conversation.Id}");

            try
            {
                string command = messageBody.Trim();
                string args = null;
                var splitterIdx = messageBody.IndexOfAny(CommandArgsSplitters);
                if (splitterIdx != -1)
                {
                    command = messageBody.Substring(0, splitterIdx).Trim();
                    args = messageBody.Substring(splitterIdx + 1).Trim();
                }
            
                
                if (command == "/connect")
                    await OnConnectCommand(e, command, args);
                else if (command == "/token")
                    await OnTokenCommand(e, command, args);
                else if (command == "/list")
                    await OnListCommand(e, command, args);
                else if (command == "/disconnect")
                    await OnDisconnectCommand(e, command, args);
            }
            catch (Exception ex)
            {
                var errorId = Utils.GenerateCryptoRandomString(10);
                await conversation.SendMessage(
                    $"{BotPrefix}Не удалось выполнить команду из-за внутренней ошибки.\nПожалуйста, свяжитесь с разработчиком (/author).\nНомер ошибки: {errorId}");
                _logger.LogError(ex, $"Failed to process command: \"{messageBody}\" [errorId={errorId}]");
            }
        }

        private Conversation _findOrInsertConversation(Conversation conversation)
        {
            var dbConversation = _conversations.FindOne(x => x.Equals(conversation));
            if (dbConversation == null)
            {
                dbConversation = conversation;
                _conversations.Insert(dbConversation);
            } else if (dbConversation.Title != conversation.Title)
            {
                _conversations.Update(conversation);
            }

            return dbConversation;
        }

        private async Task OnDisconnectCommand(Provider.MessageEventArgs e, string command, string args)
        {
            var conversation = e.Message.OriginConversation;

            if (string.IsNullOrEmpty(args))
            {
                await conversation.SendMessage(
                    $"{BotPrefix}Использование:\n/disconnect <connectionId>\n\nГде <connectionId> - ID сопряжения. Узнать его можно, введя /list");
                return;
            }

            try
            {
                var connectionId = new ObjectId(args);
                var connection = _connections
                    .Include("$.LeftConversation")
                    .Include("$.RightConversation")
                    .FindById(connectionId);
                if (connection == null) throw new ArgumentException("Connection does not exists");

                if (!connection.LeftConversation.Equals(conversation) &&
                    !connection.RightConversation.Equals(conversation))
                    throw new ArgumentException("Connection does not valid for this chat");
                var otherConversation = connection.LeftConversation.Equals(conversation)
                    ? connection.RightConversation
                    : connection.LeftConversation;

                _connections.Delete(connectionId);
                await Task.WhenAll(
                    conversation.SendMessage($"{BotPrefix}Чат {otherConversation} отключён"),
                    otherConversation.SendMessage($"{BotPrefix} Чат {conversation} отключён")
                );
            }
            catch (ArgumentException)
            {
                await conversation.SendMessage($"{BotPrefix}Сопряжение с таким ID не найдено");
            }
        }

        private async Task OnListCommand(Provider.MessageEventArgs e, string command, string args)
        {
            var conversation = _findOrInsertConversation(e.Message.OriginConversation);

            var connections = _connections
                .Include("$.LeftConversation")
                .Include("$.RightConversation")
                .FindAll()
                .Where(x => x.LeftConversation.Equals(conversation) || x.RightConversation.Equals(conversation));

            if (!connections.Any())
            {
                await conversation.SendMessage($"{BotPrefix}Нет сопряжённых чатов. Введите /start для начала");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendFormat("{0}Сопряжённые чаты:\n", BotPrefix);

            var i = 1;
            foreach (var connection in connections)
            {
                sb.AppendFormat("{0}. ", i++);

                if (connection.LeftConversation.Equals(conversation))
                    sb.Append(CurrentChatPrefix);
                if (connection.LeftConversation != null)
                    sb.Append(connection.LeftConversation);
                else
                    sb.Append("<NONE>");

                sb.Append(" <--> ");

                if (connection.RightConversation.Equals(conversation))
                    sb.Append(CurrentChatPrefix);
                if (connection.RightConversation != null)
                    sb.Append(connection.RightConversation);
                else
                    sb.Append("<NONE>");

                sb.AppendFormat(" /disconnect_{0}\n", connection.ConnectionId);
            }

            await conversation.SendMessage(sb.ToString());
        }

        private async Task OnTokenCommand(Provider.MessageEventArgs e, string command, string args)
        {
            var conversation = _findOrInsertConversation(e.Message.OriginConversation);

            var connection = new Connection();
            connection.LeftConversation = conversation;
            connection.Token = Utils.GenerateCryptoRandomString(20);
            _connections.Insert(connection);

            await conversation.SendMessage(
                $"{BotPrefix}Команда для сопряжения чатов:\n/connect $mbb2${connection.Token}\n\nВведите эту команду в другом чате, чтобы подключить его к данному чату");
        }

        private async Task OnConnectCommand(Provider.MessageEventArgs e, string command, string args)
             {

            var conversation = _findOrInsertConversation(e.Message.OriginConversation);

            if (string.IsNullOrEmpty(args))
            {
                await conversation.SendMessage(
                    $"{BotPrefix}Использование:\n/connect <token>\n\nГде <token> - ключ подключения к другому чату. Чтобы получить такой ключ, введите /token");
                return;
            }

            var token = args.Trim();
            if (!token.StartsWith("$mbb2$"))
            {
                await conversation.SendMessage($"{BotPrefix}Ключ подключения не валидный");
                return;
            }

            token = token.Substring(6);

            var connection = _connections
                .Include(x => x.LeftConversation)
                .FindOne(x => x.Token == token);
            if (connection == null)
            {
                await conversation.SendMessage($"{BotPrefix}Ключ подключения не валидный");
                return;
            }

            if (connection.RightConversation != null || connection.CreatedAt.AddHours(1) < DateTime.Now)
            {
                await conversation.SendMessage($"{BotPrefix}Ключ подключения устарел");
                return;
            }

            if (connection.LeftConversation.Equals(conversation))
            {
                await conversation.SendMessage(
                    $"{BotPrefix}Вы не можете присоединить чат к самому себе.\nПожалуйста, введите эту команду в другом чате с этим ботом");
                return;
            }

            var otherConnections = _connections.Find(x =>
                x.LeftConversation.Equals(connection.LeftConversation) && x.RightConversation.Equals(conversation) ||
                x.LeftConversation.Equals(conversation) && x.RightConversation.Equals(connection.LeftConversation)
            );

            if (otherConnections.Any())
            {
                await conversation.SendMessage($"{BotPrefix}Эти чаты уже сопряжены друг с другом");
                return;
            }

            try
            {
                await connection.LeftConversation.SendMessage(
                    $"{BotPrefix}Этот чат сопряжён с {conversation}\n/list - список всех сопряжений");
            }
            catch (Exception ex)
            {
                await conversation.SendMessage(
                    $"{BotPrefix}Невозможно подключить чат {connection.LeftConversation}: не удалось отправить тестовое сообщение (возможно бота выгнали из того чата?)");
                _logger.LogWarning(ex, "Could not send test message to conversation while connecting");
                return;
            }


            connection.RightConversation = conversation;
            _connections.Update(connection);

            await conversation.SendMessage(
                $"{BotPrefix}Этот чат сопряжён с {connection.LeftConversation}\n/list - список всех сопряжений");
        }

        private void OnMessageReceived(object sender, Provider.MessageEventArgs e)
        {
            var conversation = e.Message.OriginConversation;
            var provider = conversation.Provider;

            _logger.LogTrace(
                $"Message received from {provider.DisplayName}, conversationId: {conversation.Id}");

            var connections = _connections
                .Include(x => x.LeftConversation)
                .Include(x => x.RightConversation)
                .Find(x => x.LeftConversation.ConversationId == conversation.ConversationId ||
                           x.RightConversation.ConversationId == conversation.ConversationId);

            Task.WhenAll(connections.Select(connection =>
            {
                if (connection.Direction == ConnectionDirection.None) return Task.CompletedTask;

                Conversation otherConversation = null;
                switch (connection.Direction)
                {
                    case ConnectionDirection.TwoWay:
                        otherConversation = Equals(connection.LeftConversation, conversation)
                            ? connection.RightConversation
                            : connection.LeftConversation;
                        break;
                    case ConnectionDirection.ToLeft when Equals(connection.RightConversation, conversation):
                        otherConversation = connection.LeftConversation;
                        break;
                    case ConnectionDirection.ToRight when Equals(connection.LeftConversation, conversation):
                    {
                        otherConversation = connection.RightConversation;
                        break;
                    }
                }

                if (otherConversation != null) return otherConversation.SendMessage(e.Message);

                return Task.CompletedTask;
            }));
        }
    }
}