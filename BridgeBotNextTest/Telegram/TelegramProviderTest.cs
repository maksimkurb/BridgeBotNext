using System;
using Xunit;

namespace BridgeBotNextTest
{
    public class TelegramProviderTestingPlatformFixture : IDisposable
    {
        public TelegramProviderTestingPlatform TestingPlatform;

        public TelegramProviderTestingPlatformFixture()
        {
            var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");

            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("Please provide Telegram bot token via environment variable TELEGRAM_BOT_TOKEN");
            }

            TestingPlatform = new TelegramProviderTestingPlatform(new TestTelegramProvider(token));
        }

        public void Dispose()
        {
            TestingPlatform?.Dispose();
        }
    }

    public class TelegramProviderTest : IClassFixture<TelegramProviderTestingPlatformFixture>
    {
        public TelegramProviderTest(TelegramProviderTestingPlatformFixture fixture)
        {
            _fixture = fixture;
        }

        private readonly TelegramProviderTestingPlatformFixture _fixture;

        [Fact]
        public async void PlainMessageTest()
        {
            Assert.True(await _fixture.TestingPlatform.PlainMessage());
        }
        
        [Fact]
        public async void MediaAttachmentTest()
        {
            Assert.True(await _fixture.TestingPlatform.MediaAttachment());
        }
        
        [Fact]
        public async void AlbumAttachmentTest()
        {
            Assert.True(await _fixture.TestingPlatform.AlbumAttachment());
        }

        [Fact]
        public async void OtherAttachmentsMessageTest()
        {
            Assert.True(await _fixture.TestingPlatform.OtherAttachments());
        }
    }
}