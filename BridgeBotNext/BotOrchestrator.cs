using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BridgeBotNext.Configuration;
using BridgeBotNext.Entities;
using BridgeBotNext.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BridgeBotNext
{
    public class BotOrchestrator
    {
        private static readonly char[] CommandArgsSplitters = {' ', '_'};
        private static readonly string BotPrefix = "🔹 ";
        private static readonly string CurrentChatPrefix = "📍";
        private readonly IOptions<AuthConfiguration> _authConfiguration;
        private readonly ILogger<BotOrchestrator> _logger;
        private readonly List<Provider> _providers = new List<Provider>();

        private readonly BotDbContext _db;

        public BotOrchestrator(ILogger<BotOrchestrator> logger, BotDbContext db,
            IOptions<AuthConfiguration> authConfiguration)
        {
            _logger = logger;
            _db = db;
            _authConfiguration = authConfiguration;

            if (_authConfiguration.Value == null ||
                _authConfiguration.Value.Enabled && String.IsNullOrEmpty(_authConfiguration.Value.Password))
            {
                throw new Exception(
                    "Incorrect Auth settings. Auth is not configured or auth is enabled, but password is empty");
            }
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
                $"Command received from {conversation.Provider.DisplayName}, conversationId: {conversation.OriginId}");

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

                if (command == "/auth")
                    await OnAuthCommand(e, command, args);
                else if (command == "/start")
                    await OnStartCommand(e, command, args);
                else if (command == "/connect") // allow everybody to use connection token (make setup process easier)
                    await OnConnectCommand(e, command, args);
                else if (command == "/deauth" && await _ensureHasAdminRights(e))
                    await OnDeauthCommand(e, command, args);
                else if (command == "/token" && await _ensureHasAdminRights(e))
                    await OnTokenCommand(e, command, args);
                else if (command == "/list"
                ) // allow everybody to use list command (everybody have rights to see where messages are resending)
                    await OnListCommand(e, command, args);
                else if (command == "/disconnect" && await _ensureHasAdminRights(e))
                    await OnDisconnectCommand(e, command, args);
                else
                {
                    _logger.LogTrace(
                        $"Unknown command: {command}");
                }
            }
            catch (Exception ex)
            {
                var errorId = Utils.GenerateCryptoRandomString(10);
                await conversation.SendMessage(
                    $"{BotPrefix}Не удалось выполнить команду из-за внутренней ошибки.\nПожалуйста, создайте тикет на странице проекта (https://github.com/maksimkurb/BridgeBotNext) и лог-файл.\nНомер ошибки, для поиска в логах: {errorId}");
                _logger.LogError(ex, $"Failed to process command: \"{messageBody}\" [errorId={errorId}]");
            }
        }

        private Conversation _findOrInsertConversation(Conversation conversation)
        {
            var dbConversation = _db.Conversations.Find(conversation.ConversationId);
            if (dbConversation == null)
            {
                dbConversation = conversation;
                _db.Conversations.Add(dbConversation);
                _db.SaveChanges();
            }
            else if (dbConversation.Title != conversation.Title)
            {
                _db.Conversations.Update(conversation);
                _db.SaveChanges();
            }

            return dbConversation;
        }

        private Person _findPerson(ProviderId providerId)
        {
            return _db.Persons.Find(providerId);
        }

        private async Task<bool> _ensureHasAdminRights(Provider.MessageEventArgs e)
        {
            if (!_authConfiguration.Value.Enabled) return true;

            if (e.Message.OriginSender.IsAdmin)
            {
                return true;
            }

            var person = _findPerson(e.Message.OriginSender.PersonId);
            if (person != null && person.IsAdmin)
            {
                return true;
            }

            _logger.LogTrace(
                $"Command execution access denied for {e.Message.OriginSender.ProfileUrl}");
            await e.Message.OriginConversation.SendMessage(
                $"{BotPrefix}Недостаточно прав для выполнения команды. Авторизуйтесь через /auth <пароль бота> (написать можно в ЛС, я запомню)");

            return false;
        }


        private async Task OnStartCommand(Provider.MessageEventArgs e, string command, string args)
        {
            await e.Message.OriginConversation.SendMessage(
                $@"{BotPrefix}Привет! Я BridgeBotNext!
Я помогу объединить беседы из различных мессенджеров, путём пересылки сообщений из одного в другой и обратно.
1) Убедитесь, что у бота есть доступ ко всем сообщениям в беседе.
2) Используйте команду /token в основном чате, чтобы получить команду для соединения с другим чатом.
3) Введите полученную команду в другой беседе, где находится этот бот.
Вы можете посмотреть текущие соединения с помощью команды /list

/Поддерживаемые_мессенджеры: {string.Join(", ", _providers.Select(p => p.DisplayName))}
/Версия_бота: {Program.Version}
/Страница_проекта: https://github.com/maksimkurb/BridgeBotNext
/Автор: <Maxim Kurbatov> max@cubly.ru, 2018-2019");
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
                var connectionId = long.Parse(args);
                var connection = _db.Connections
                    .Include(c => c.LeftConversation)
                    .Include(c => c.RightConversation)
                    .SingleOrDefault(c => c.ConnectionId == connectionId);
                if (connection == null) throw new ArgumentException("Connection does not exists");

                if (!connection.LeftConversation.Equals(conversation) &&
                    !connection.RightConversation.Equals(conversation))
                    throw new ArgumentException("Connection does not valid for this chat");
                var otherConversation = connection.LeftConversation.Equals(conversation)
                    ? connection.RightConversation
                    : connection.LeftConversation;

                _db.Connections.Remove(connection);
                await _db.SaveChangesAsync();
                
                await Task.WhenAll(
                    conversation.SendMessage($"{BotPrefix}Чат {otherConversation} отключён"),
                    otherConversation.SendMessage($"{BotPrefix} Чат {conversation} отключён")
                );
            }
            catch (Exception ex) when (ex is ArgumentException || ex is FormatException)
            {
                await conversation.SendMessage($"{BotPrefix}Сопряжение с таким ID не найдено");
            }
        }

        private async Task OnListCommand(Provider.MessageEventArgs e, string command, string args)
        {
            var conversation = _findOrInsertConversation(e.Message.OriginConversation);

            var connections = _db.Connections
                .Include(x => x.LeftConversation)
                .Include(x => x.RightConversation)
                .Where(x => x.LeftConversation.Equals(conversation) || x.RightConversation.Equals(conversation))
                .ToList();

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

                if (connection.LeftConversation == conversation)
                    sb.Append(CurrentChatPrefix);
                if (connection.LeftConversation != null)
                    sb.Append(connection.LeftConversation);
                else
                    sb.Append("<NONE>");

                sb.Append(" <--> ");

                if (connection.RightConversation == conversation)
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
            _db.Connections.Add(connection);
            await _db.SaveChangesAsync();

            await conversation.SendMessage(
                $"{BotPrefix}Команда для сопряжения чатов:\n/connect $mbb2${connection.Token}\n\nВведите эту команду в другом чате, чтобы подключить его к данному чату");
        }

        private async Task OnAuthCommand(Provider.MessageEventArgs e, string command, string args)
        {
            var conversation = e.Message.OriginConversation;
            if (!_authConfiguration.Value.Enabled)
            {
                await conversation.SendMessage($"{BotPrefix}Авторизация для этого бота не требуется");
                return;
            }

            if (args.Equals(_authConfiguration.Value.Password))
            {
                var person = _findPerson(e.Message.OriginSender.PersonId);
                if (person != null && person.IsAdmin)
                {
                    await conversation.SendMessage($"{BotPrefix}Пользователь уже является администратором");
                    return;
                }

                person = e.Message.OriginSender;
                person.IsAdmin = true;
                _db.Persons.Add(person);
                await _db.SaveChangesAsync();
                
                await conversation.SendMessage(
                    $"{BotPrefix}Пользователь {person.DisplayName} [{person.ProfileUrl}] теперь администратор");
            }
            else
            {
                await conversation.SendMessage($"{BotPrefix}Неправильный пароль");
            }
        }

        private async Task OnDeauthCommand(Provider.MessageEventArgs e, string command, string args)
        {
            var conversation = e.Message.OriginConversation;
            if (!_authConfiguration.Value.Enabled)
            {
                await conversation.SendMessage($"{BotPrefix}Авторизация для этого бота не требуется");
                return;
            }

            var providerId = string.IsNullOrEmpty(args)
                ? e.Message.OriginSender.PersonId
                : new ProviderId(e.Message.OriginSender.Provider, args);
            var personToDemote = _findPerson(providerId);
            if (personToDemote != null)
            {
                _db.Persons.Remove(personToDemote);
                await _db.SaveChangesAsync();
                await conversation.SendMessage(
                    $"{BotPrefix}Пользователь {personToDemote.DisplayName} [{e.Message.OriginSender.ProfileUrl}] больше не администратор");
            }
            else
            {
                await conversation.SendMessage($"{BotPrefix}Пользователь не найден");
            }
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
                await conversation.SendMessage($"{BotPrefix}Ключ подключения не валидный (возможно в нём опечатка?)");
                return;
            }

            token = token.Substring(6);

            var connection = _db.Connections.First(x => x.Token == token);
            
            if (connection == null)
            {
                await conversation.SendMessage($"{BotPrefix}Ключ подключения не не существует");
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

            var otherConnections = _db.Connections.Where(x =>
                x.LeftConversation.Equals(connection.LeftConversation) && x.RightConversation.Equals(conversation) ||
                x.LeftConversation.Equals(conversation) && x.RightConversation.Equals(connection.LeftConversation)
            ).ToList();

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
            _db.Connections.Update(connection);
            await _db.SaveChangesAsync();

            await conversation.SendMessage(
                $"{BotPrefix}Этот чат сопряжён с {connection.LeftConversation}\n/list - список всех сопряжений");
        }

        private void OnMessageReceived(object sender, Provider.MessageEventArgs e)
        {
            var conversation = e.Message.OriginConversation;
            var provider = conversation.Provider;

            _logger.LogTrace(
                $"Message received from {provider.DisplayName}, conversationId: {conversation.OriginId}");

            var connections = _db.Connections
                .Include(x => x.LeftConversation)
                .Include(x => x.RightConversation)
                .Where(x => Equals(x.LeftConversation.ConversationId, conversation.ConversationId) ||
                           Equals(x.RightConversation.ConversationId, conversation.ConversationId))
                .ToList();

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
