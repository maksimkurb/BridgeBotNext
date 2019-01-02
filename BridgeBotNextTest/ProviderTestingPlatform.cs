using System;
using System.Threading.Tasks;
using BridgeBotNext;
using BridgeBotNext.Attachments;
using BridgeBotNext.Providers;
using BridgeBotNext.Providers.Tg;

namespace BridgeBotNextTest
{
    public class ProviderTestingPlatform : IDisposable
    {
        protected Conversation _conversation;
        protected Provider _provider;
        private bool _started;
        private TaskCompletionSource<bool> tcs;

        public ProviderTestingPlatform(Provider provider)
        {
            _provider = provider;
            _provider.CommandReceived += OnCommand;
            _provider.Connect();

            tcs = new TaskCompletionSource<bool>();
        }

        public void Dispose()
        {
            _provider.Dispose();
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
         * Test case 2: forwarded messages
         */
        public async Task<bool> ForwardedMessages()
        {
            await StartTest("Should send forwarded messages");
            var sender1 = new TgPerson(null, "BotFather", "Person 1");
            var sender2 = new TgPerson(null, "BotFather", "Person 2");
            var sender3 = new TgPerson(null, "BotFather", "Person 3");

            var msg = new Message(originSender: sender1, body: "Test body", forwardedMessages: new[]
            {
                new Message(originSender: sender1, body: "Test fwd 1\nMultiline\nMessage\nTest",
                    forwardedMessages: new[]
                    {
                        new Message(originSender: sender3, body: "Below this line there should not be a username"),
                        new Message(originSender: sender3, body: "Now it should be"),
                    }),

                new Message(originSender: sender2, body: "Test fwd 2", forwardedMessages: new[]
                {
                    new Message(originSender: sender2, body: "Test fwd2.1", forwardedMessages: new[]
                    {
                        new Message(originSender: sender2, body: "Test fwd2.1.1"),
                    }),
                    new Message(originSender: sender2, body: "Test fwd2.2", forwardedMessages: new[]
                    {
                        new Message(originSender: sender2, body: "Test fwd2.2.1"),
                    }),
                    new Message(originSender: sender3, body: "Test fwd2.3"),
                }),
            });

            await _provider.SendMessage(_conversation, msg);

            return await WaitResults();
        }

        /**
         * Test case 3: media attachment
         */
        public async Task<bool> MediaAttachment()
        {
            await StartTest("Should send photo attachment, then video attachment");

            await _provider.SendMessage(_conversation, new Message(attachments: new[]
            {
                new PhotoAttachment($"https://cataas.com/cat?v={DateTime.UnixEpoch.Ticks}")
            }));
            await _provider.SendMessage(_conversation, new Message(attachments: new[]
            {
                new VideoAttachment("http://techslides.com/demos/sample-videos/small.mp4")
            }));

            return await WaitResults();
        }

        /**
         * Test case 4: album attachment
         */
        public async Task<bool> AlbumAttachment()
        {
            await StartTest("Should 2 photos and 1 video as album (if supported by messenger) with captions");

            await _provider.SendMessage(_conversation, new Message(attachments: new Attachment[]
            {
                new PhotoAttachment($"https://cataas.com/cat?v={DateTime.Now.Ticks}", caption: "photo 1"),
                new PhotoAttachment($"https://cataas.com/cat?v={DateTime.Now.Ticks + 10}",
                    caption: "photo 2"),
                new VideoAttachment("http://techslides.com/demos/sample-videos/small.mp4", caption: "video 1")
            }));

            return await WaitResults();
        }

        /**
         * Test case 4: other attachment types
         */
        public async Task<bool> OtherAttachments()
        {
            await StartTest("Should send variety of attachments");

            await _provider.SendMessage(_conversation, "audio");
            await _provider.SendMessage(_conversation, new Message(attachments: new[]
            {
                new AudioAttachment("https://ccrma.stanford.edu/~jos/mp3/gtr-nylon22.mp3", caption: "nice cheering",
                    title: "Cheering", performer: "Crowd"),
            }));

            await Task.Delay(1000);

            await _provider.SendMessage(_conversation, "animation");
            await _provider.SendMessage(_conversation, new Message(attachments: new[]
            {
                new AnimationAttachment("http://techslides.com/demos/sample-videos/small.mp4"),
            }));

            await Task.Delay(1000);

            await _provider.SendMessage(_conversation, "contact");
            await _provider.SendMessage(_conversation, new Message(attachments: new[]
            {
                new ContactAttachment("John", "Doe", "+78005553535", "john.doe@example.com"),
            }));

            await Task.Delay(1000);

            await _provider.SendMessage(_conversation, "place");
            await _provider.SendMessage(_conversation, new Message(attachments: new[]
            {
                new PlaceAttachment(-72.020112, 14.452641),
            }));

            await Task.Delay(1000);

            await _provider.SendMessage(_conversation, "file");
            await _provider.SendMessage(_conversation, new Message(attachments: new[]
            {
                new FileAttachment("https://sample-videos.com/text/Sample-text-file-10kb.txt",
                    caption: "Sample text file"),
            }));

            await Task.Delay(1000);

            await _provider.SendMessage(_conversation, "link");
            await _provider.SendMessage(_conversation, new Message(attachments: new[]
            {
                new LinkAttachment("https://google.com"),
            }));

            await Task.Delay(1000);

            await _provider.SendMessage(_conversation, "sticker");
            await _provider.SendMessage(_conversation, new Message(attachments: new[]
            {
                new StickerAttachment("https://www.gstatic.com/webp/gallery3/2_webp_ll.webp"),
            }));

            await Task.Delay(1000);

            await _provider.SendMessage(_conversation, "voice");
            await _provider.SendMessage(_conversation, new Message(attachments: new[]
            {
                new VoiceAttachment("https://upload.wikimedia.org/wikipedia/commons/c/c8/Example.ogg"),
            }));

            return await WaitResults();
        }


        /**
         * Test case 5: forwarded messages
         */
        public async Task<bool> ForwardedMessagesWithAttachments()
        {
            await StartTest("Should send forwarded messages and attachments");
            var sender1 = new TgPerson(null, "BotFather", "Person 1");
            var sender2 = new TgPerson(null, "BotFather", "Person 2");
            var sender3 = new TgPerson(null, "BotFather", "Person 3");

            var msg = new Message(originSender: sender1, body: "Test body", forwardedMessages: new[]
            {
                new Message(originSender: sender1, body: "Here is 3 pics too", forwardedMessages: new[]
                {
                    new Message(originSender: sender3, body: "Here is 3 pics", attachments: new[]
                    {
                        new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+1.1"),
                        new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+1.2"),
                        new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+1.3")
                    }),
                }, attachments: new[]
                {
                    new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+2.1"),
                    new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+2.2"),
                    new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+2.3")
                }),
                new Message(originSender: sender2, body: "Test fwd 2", forwardedMessages: new[]
                {
                    new Message(originSender: sender3, body: "Here is 11 msgs", attachments: new[]
                    {
                        new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+3.1"),
                        new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+3.2"),
                        new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+3.3"),
                        new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+3.4"),
                        new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+3.5"),
                        new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+3.6"),
                        new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+3.7"),
                        new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+3.8"),
                        new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+3.9"),
                        new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+3.10"),
                        new PhotoAttachment($"https://dummyimage.com/600x400/000/ffffff?text=pic+3.11"),
                    }),
                }),
            });

            await _provider.SendMessage(_conversation, msg);

            return await WaitResults();
        }

        private async void OnCommand(object sender, Provider.MessageEventArgs e)
        {
            Console.WriteLine(e.Message.Body);
            if (!_started && e.Message.Body.ToLower().StartsWith("/test"))
            {
                _conversation = e.Message.OriginConversation;
                _started = true;
                await _provider.SendMessage(_conversation, "🔹 Test started 🔹");
                tcs.SetResult(true);
            }
            else if (e.Message.Body.ToLower().StartsWith("/pass"))
            {
                tcs?.SetResult(true);
            }
            else if (e.Message.Body.ToLower().StartsWith("/fail"))
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

        public virtual async Task<bool> WaitResults(bool sendPassOrFail = true)
        {
            if (sendPassOrFail)
            {
                await _provider.SendMessage(_conversation, "🔹 /pass or /fail 🔹");
            }

            var res = await tcs.Task;
            tcs = null;
            return res;
        }
    }
}