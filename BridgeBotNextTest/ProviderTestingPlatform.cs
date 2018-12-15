using System;
using System.Threading.Tasks;
using BridgeBotNext;
using BridgeBotNext.Attachments;
using BridgeBotNext.Providers;

namespace BridgeBotNextTest
{
    public class ProviderTestingPlatform: IDisposable
    {
        protected Provider _provider;
        protected Conversation _conversation;
        private TaskCompletionSource<bool> tcs;
        private bool _started = false;

        public ProviderTestingPlatform(Provider provider)
        {
            _provider = provider;
            _provider.CommandReceived += OnCommand;
            _provider.Connect();
            
            tcs = new TaskCompletionSource<bool>();
        }
        
        /**
         * Test case 1: plain message
         */
        public async Task<bool> PlainMessage()
        {
            await StartTest("Should send plain message");
            
            await _provider.SendMessage(_conversation, "Plain message. Простое сообщение. 简单的消息.");
            
            return await WaitResults();
        }
        
        /**
         * Test case 2: media attachment
         */
        public async Task<bool> MediaAttachment()
        {
            await StartTest("Should send photo attachment, then video attachment");
            
            await _provider.SendMessage(_conversation, new Message(attachments: new []
            {
                new PhotoAttachment($"https://cataas.com/cat?v={DateTime.UnixEpoch.Ticks}"), 
            }));
            await _provider.SendMessage(_conversation, new Message(attachments: new []
            {
                new VideoAttachment("http://techslides.com/demos/sample-videos/small.mp4"), 
            }));
            
            return await WaitResults();
        }

        /**
         * Test case 3: album attachment
         */
        public async Task<bool> AlbumAttachment()
        {
            await StartTest("Should 2 photos and 1 video as album (if supported by messenger) with captions");
            
            await _provider.SendMessage(_conversation, new Message(attachments: new []
            {
                new AlbumAttachment(new IAlbumableAttachment[]
                {   
                    new PhotoAttachment($"https://cataas.com/cat?v={DateTime.UnixEpoch.Ticks}", caption: "photo 1"),
                    new PhotoAttachment($"https://cataas.com/cat?v={DateTime.UnixEpoch.Ticks + 10}", caption: "photo 2"),
                    new VideoAttachment("http://techslides.com/demos/sample-videos/small.mp4", caption: "video 1"),
                }), 
            }));
            
            return await WaitResults();
        }

        private async void OnCommand(object sender, Provider.MessageEventArgs e)
        {
            Console.WriteLine(e.Message.Body);
            if (!_started && e.Message.Body.ToLower().StartsWith("/test"))
            {
                _conversation = e.Message.OriginConversation;
                _started = true;
                tcs.SetResult(true);
                await _provider.SendMessage(_conversation, "🔹 Test started 🔹");
            } else if (e.Message.Body.ToLower().StartsWith("/pass"))
            {
                tcs?.SetResult(true);
            } else if (e.Message.Body.ToLower().StartsWith("/fail"))
            {
                tcs?.SetResult(false);
            }
        }

        public virtual async Task StartTest(string testCase)
        {
            if (!_started)
                await tcs.Task;
            
            tcs = new TaskCompletionSource<bool>();
            await _provider.SendMessage(_conversation, $"🔹 Test case:\n{testCase}");
        }

        public virtual async Task<bool> WaitResults()
        {
            bool res = await tcs.Task;
            tcs = null;
            return res;
        }

        public void Dispose()
        {
            _provider.Dispose();
        }
    }
}