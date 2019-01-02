using System;
using BridgeBotNext.Configuration;
using BridgeBotNext.Providers.Vk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BridgeBotNextTest.Telegram
{
    public class VkProviderTestingPlatformFixture : IDisposable
    {
        public ProviderTestingPlatform TestingPlatform;

        public VkProviderTestingPlatformFixture()
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            TestingPlatform = new ProviderTestingPlatform(serviceProvider.GetService<VkProvider>());
        }

        public void Dispose()
        {
            TestingPlatform?.Dispose();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables("BOT_")
                .Build();

            services
                .AddLogging(configure => configure.AddConsole())
                .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Trace);

            services.Configure<VkConfiguration>(config.GetSection("Vk"));

            services.AddSingleton<VkProvider>();
        }
    }

    public class VkProviderTest : IClassFixture<VkProviderTestingPlatformFixture>
    {
        public VkProviderTest(VkProviderTestingPlatformFixture fixture)
        {
            _fixture = fixture;
        }

        private readonly VkProviderTestingPlatformFixture _fixture;

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