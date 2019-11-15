/**
 * Original source: https://github.com/truecooler/VkBotFramework
 * Author: TheCooler

    MIT License
    
    Copyright (c) 2018 TheCooler
    
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:
    
    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.
    
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Exception;
using VkNet.Model;
using VkNet.Model.GroupUpdate;
using VkNet.Model.RequestParams;

namespace BridgeBotNext.Providers.Vk
{
    public class VkLongPoll : IDisposable
    {
        private CancellationTokenSource _cancellationToken;
        private ulong _groupId;
        private ILogger _logger;
        private LongPollServerResponse _pollSettings;

        private IVkApi _vkApi;

        public VkLongPoll(IVkApi vkApi, ulong groupId, ILogger<VkLongPoll> logger)
        {
            _vkApi = vkApi;
            _groupId = groupId;
            _logger = logger;
        }

        public void Dispose()
        {
            _cancellationToken.Cancel();
        }

        public event EventHandler<GroupUpdateReceivedEventArgs> OnGroupUpdateReceived;
        public event EventHandler<MessageReceivedEventArgs> OnMessageReceived;

        private void _getLongPollSettings()
        {
            _pollSettings = _vkApi.Groups.GetLongPollServer(_groupId);
            _logger.LogInformation($"Got new long poll settings. Ts: {_pollSettings.Ts}");
        }

        private T _checkForErrors<T>(Task<T> task)
        {
            if (task.IsFaulted)
            {
                if (task.Exception is AggregateException ae)
                    foreach (var ex in ae.InnerExceptions)
                        switch (ex)
                        {
                            case LongPollOutdateException e:
                                _pollSettings.Ts = e.Ts;
                                return default(T);
                            case LongPollKeyExpiredException _:
                                _getLongPollSettings();
                                return default(T);
                            case LongPollInfoLostException _:
                                _getLongPollSettings();
                                return default(T);
                            default:
                                _logger.LogError(ex, "Error occurred during long-polling");
                                throw ex;
                        }

                if (task.Exception != null)
                {
                    _logger.LogError(task.Exception, "Error occurred during long-polling");
                    throw task.Exception;
                }
            }

            if (task.IsCanceled)
            {
                _logger.LogWarning("Timeout reached during polling handling");
                return default(T);
            }

            try
            {
                return task.Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not return task result");
                throw;
            }
        }

        private void _processEvents(BotsLongPollHistoryResponse pollResponse)
        {
            foreach (var update in pollResponse.Updates)
            {
                OnGroupUpdateReceived?.Invoke(this, new GroupUpdateReceivedEventArgs(update));
                if (update.Type == GroupUpdateType.MessageNew)
                {
                    OnMessageReceived?.Invoke(this, new MessageReceivedEventArgs(update.Message));
                }
            }
        }


        private async Task _startPolling()
        {
            _getLongPollSettings();
            while (true)
                try
                {
                    if (_cancellationToken.IsCancellationRequested) break;
                    var longPollResponse = await ((Task) _vkApi.Groups.GetBotsLongPollHistoryAsync(
                            new BotsLongPollHistoryParams
                            {
                                Key = _pollSettings.Key,
                                Server = _pollSettings.Server,
                                Ts = _pollSettings.Ts,
                                Wait = 25
                            })
                        .ContinueWith(_checkForErrors)).ConfigureAwait(false);
                    if (longPollResponse == default(BotsLongPollHistoryResponse))
                        continue;
                    _processEvents(longPollResponse);
                    _pollSettings.Ts = longPollResponse.Ts;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during poll iteration");
                    throw;
                }
        }

        public void StartPolling()
        {
            if (_cancellationToken == null)
            {
                _logger.LogInformation("Starting vk long-poll");
                _cancellationToken = new CancellationTokenSource();
                Task.Run(_startPolling);
            }
            else
            {
                _logger.LogWarning("Could not start vk long-poll: already started");
            }
        }

        public class MessageReceivedEventArgs : EventArgs
        {
            public Message message;

            public MessageReceivedEventArgs(Message message)
            {
                this.message = message;
            }
        }

        public class GroupUpdateReceivedEventArgs : EventArgs
        {
            public GroupUpdate update;

            public GroupUpdateReceivedEventArgs(GroupUpdate update)
            {
                this.update = update;
            }
        }
    }
}