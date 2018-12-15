using System;
using Xunit;

namespace BridgeBotNextTest
{
    public class TelegramProviderTestingPlatformFixture : IDisposable
    {
        public TelegramProviderTestingPlatform TestingPlatform;
        public TelegramProviderTestingPlatformFixture()
        {
            TestingPlatform = new TelegramProviderTestingPlatform(new TestTelegramProvider("655186440:AAFd9V67Mscx3YLxxUsx1VTwdllzyKS7FVQ"));
        }

        public void Dispose()
        {
            TestingPlatform?.Dispose();
        }
    }
    
    public class TelegramProviderTest: IClassFixture<TelegramProviderTestingPlatformFixture>
    {

        private TelegramProviderTestingPlatformFixture _fixture;

        public TelegramProviderTest(TelegramProviderTestingPlatformFixture fixture)
        {
            _fixture = fixture;
        }

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
    }
}