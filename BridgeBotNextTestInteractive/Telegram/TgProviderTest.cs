using System;
using BridgeBotNext.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BridgeBotNextTest.Telegram
{
    public class TgProviderTestingPlatformFixture : IDisposable
    {
        public TgProviderTestingPlatform TestingPlatform;

        public TgProviderTestingPlatformFixture()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            TestingPlatform = new TgProviderTestingPlatform(serviceProvider.GetService<TestTgProvider>());
        }

        public void Dispose()
        {
            TestingPlatform?.Dispose();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, false)
                .AddEnvironmentVariables("BOT_")
                .Build();

            services
                .AddLogging(configure => configure.AddConsole())
                .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Trace);

            services.Configure<TgConfiguration>(config.GetSection("Tg"));

            services.AddSingleton<TestTgProvider>();
        }
    }

    public class TgProviderTest : IClassFixture<TgProviderTestingPlatformFixture>
    {
        public TgProviderTest(TgProviderTestingPlatformFixture fixture)
        {
            _fixture = fixture;
        }

        private readonly TgProviderTestingPlatformFixture _fixture;

        [Fact]
        public async void AlbumAttachmentTest()
        {
            Assert.True(await _fixture.TestingPlatform.AlbumAttachment());
        }

        [Fact]
        public async void ForwardedMessagesTest()
        {
            Assert.True(await _fixture.TestingPlatform.ForwardedMessages());
        }


        [Fact]
        public async void ForwardedMessagesWithAttachmentsTest()
        {
            Assert.True(await _fixture.TestingPlatform.ForwardedMessagesWithAttachments());
        }

        [Fact]
        public async void MediaAttachmentTest()
        {
            Assert.True(await _fixture.TestingPlatform.MediaAttachment());
        }

        [Fact]
        public async void OtherAttachmentsMessageTest()
        {
            Assert.True(await _fixture.TestingPlatform.OtherAttachments());
        }

        [Fact]
        public async void PlainMessageTest()
        {
            Assert.True(await _fixture.TestingPlatform.PlainMessage());
        }
    }
}