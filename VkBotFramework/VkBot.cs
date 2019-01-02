using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using VkNet;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace VkBotFramework
{
    public partial class VkBot : IDisposable
    {
        private readonly List<PhraseTemplate> _phraseTemplates = new List<PhraseTemplate>();
        public IVkApi Api;

        public long GroupId;

        protected ILogger<VkBot> Logger;
        public LongPollServerResponse PollSettings;

        public VkBot(IServiceCollection serviceCollection = null)
        {
            SetupDependencies(serviceCollection);
        }

        public VkBot(string accessToken, int groupId, IServiceCollection serviceCollection = null) : this(
            serviceCollection)
        {
            Setup(accessToken, groupId);
        }

        public VkBot(ILogger<VkBot> logger)
        {
            var container = new ServiceCollection();

            if (logger != null) container.TryAddSingleton(logger);

            SetupDependencies(container);
        }

        public VkBot(string accessToken, int groupId, ILogger<VkBot> logger) : this(logger)
        {
            Setup(accessToken, groupId);
        }


        public void Dispose()
        {
            Api.Dispose();
        }

        public event EventHandler<GroupUpdateReceivedEventArgs> OnGroupUpdateReceived;
        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;

        private void RegisterDefaultDependencies(IServiceCollection container)
        {
            if (container.All(x => x.ServiceType != typeof(ILogger<>)))
                container.TryAddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            if (container.All(x => x.ServiceType != typeof(IVkApi)))
            {
                var vkApiByDefault = new VkApi();
                vkApiByDefault.RestClient.Timeout = TimeSpan.FromSeconds(30);
                vkApiByDefault.RequestsPerSecond = 20; //  для группового access token
                container.TryAddSingleton<IVkApi>(x => vkApiByDefault);
            }
        }

        private void Setup(string accessToken, int groupId)
        {
            Api.Authorize(new ApiAuthParams
            {
                AccessToken = accessToken
            });

            GroupId = groupId;

            //Api.RestClient.Timeout = TimeSpan.FromSeconds(30);

            //ServicePointManager.UseNagleAlgorithm = false;
            //ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 20; // ограничение параллельных соединений для HttpClient
            //ServicePointManager.EnableDnsRoundRobin = true;
            //ServicePointManager.ReusePort = true;
        }

        private void SetupDependencies(IServiceCollection serviceCollection = null)
        {
            var container = serviceCollection ?? new ServiceCollection();
            RegisterDefaultDependencies(container);
            IServiceProvider serviceProvider = container.BuildServiceProvider();
            Logger = serviceProvider.GetService<ILogger<VkBot>>();
            Api = serviceProvider.GetService<IVkApi>(); //new VkApi(container);
            Logger.LogInformation("Все зависимости подключены.");
        }

        private void SetupLongPoll()
        {
            PollSettings = Api.Groups.GetLongPollServer((ulong) GroupId);
            Logger.LogInformation($"VkBot: LongPoolSettings received. ts: {PollSettings.Ts}");
        }

        public void RegisterPhraseTemplate(string regexPattern, string answer,
            RegexOptions phraseRegexPatternOptions = RegexOptions.IgnoreCase)
        {
            _phraseTemplates.Add(new PhraseTemplate(regexPattern, answer, phraseRegexPatternOptions));
        }

        public void RegisterPhraseTemplate(string regexPattern, List<string> answers,
            RegexOptions phraseRegexPatternOptions = RegexOptions.IgnoreCase)
        {
            _phraseTemplates.Add(new PhraseTemplate(regexPattern, answers, phraseRegexPatternOptions));
        }

        public void RegisterPhraseTemplate(string regexPattern, Action<Message> callback,
            RegexOptions phraseRegexPatternOptions = RegexOptions.IgnoreCase)
        {
            _phraseTemplates.Add(new PhraseTemplate(regexPattern, callback, phraseRegexPatternOptions));
        }

        protected void SearchPhraseAndHandle(Message message)
        {
            foreach (var pair in _phraseTemplates)
            {
                var regex = new Regex(pair.PhraseRegexPattern, pair.PhraseRegexPatternOptions);
                if (regex.IsMatch(message.Text))
                {
                    if (pair.Callback == null)
                        // TODO: сделать этот вызов асинхронным
                        Api.Messages.Send(new MessagesSendParams
                        {
                            Message = pair.Answers[new Random().Next(0, pair.Answers.Count)], PeerId = message.PeerId
                        });
                    else
                        pair.Callback(message);
                }
            }
        }

        protected void ProcessLongPollEvents(BotsLongPollHistoryResponse pollResponse)
        {
            foreach (var update in pollResponse.Updates)
            {
                OnGroupUpdateReceived?.Invoke(this, new GroupUpdateReceivedEventArgs(update));
                if (update.Type == GroupUpdateType.MessageNew)
                {
                    OnMessageReceived?.Invoke(this, new MessageReceivedEventArgs(update.Message));
                    SearchPhraseAndHandle(update.Message);
                }
            }
        }

        private T CheckLongPollResponseForErrorsAndHandle<T>(Task<T> task)
        {
            if (task.IsFaulted)
            {
                if (task.Exception is AggregateException ae)
                    foreach (var ex in ae.InnerExceptions)
                        if (ex is LongPollOutdateException lpoex)
                        {
                            PollSettings.Ts = lpoex.Ts;
                            return default(T);
                        }
                        else if (ex is LongPollKeyExpiredException)
                        {
                            SetupLongPoll();
                            return default(T);
                        }
                        else if (ex is LongPollInfoLostException)
                        {
                            SetupLongPoll();
                            return default(T);
                        }
                        else
                        {
                            Console.WriteLine(ex.Message);
                            throw ex;
                        }

                if (task.Exception != null)
                {
                    Logger.LogError(task.Exception.Message);
                    throw task.Exception;
                }
            }

            if (task.IsCanceled)
            {
                Logger.LogWarning(
                    "CheckLongPollResponseForErrorsAndHandle() : task.IsCanceled, possibly timeout reached");
                return default(T);
            }

            try
            {
                return task.Result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                throw;
            }
        }


        public async Task StartAsync()
        {
            SetupLongPoll();
            while (true)
                try
                {
                    var longPollResponse = await Api.Groups.GetBotsLongPollHistoryAsync(
                        new BotsLongPollHistoryParams
                        {
                            Key = PollSettings.Key,
                            Server = PollSettings.Server,
                            Ts = PollSettings.Ts,
                            Wait = 25
                        }).ContinueWith(CheckLongPollResponseForErrorsAndHandle).ConfigureAwait(false);
                    if (longPollResponse == default(BotsLongPollHistoryResponse))
                        continue;
                    //Console.WriteLine(JsonConvert.SerializeObject(longPollResponse));
                    ProcessLongPollEvents(longPollResponse);
                    PollSettings.Ts = longPollResponse.Ts;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.Message);
                    throw;
                }
        }

        public void Start()
        {
            StartAsync().GetAwaiter().GetResult();
        }
    }
}