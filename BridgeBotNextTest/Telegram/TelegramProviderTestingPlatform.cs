using System.Threading.Tasks;
using BridgeBotNext.Providers;
using BridgeBotNext.Providers.Tg;

namespace BridgeBotNextTest
{
    public class TelegramProviderTestingPlatform: ProviderTestingPlatform
    {
        public TelegramProviderTestingPlatform(TestTelegramProvider provider) : base(provider)
        {
        }

        public override Task<bool> WaitResults()
        {
            TestTelegramProvider provider = (TestTelegramProvider)_provider;
            provider.SendTestButtons(_conversation);
            return base.WaitResults();
        }
    }
}