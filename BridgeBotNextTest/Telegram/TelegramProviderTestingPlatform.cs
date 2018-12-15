using System.Threading.Tasks;

namespace BridgeBotNextTest
{
    public class TelegramProviderTestingPlatform : ProviderTestingPlatform
    {
        public TelegramProviderTestingPlatform(TestTelegramProvider provider) : base(provider)
        {
        }

        public override Task<bool> WaitResults()
        {
            var provider = (TestTelegramProvider) _provider;
            provider.SendTestButtons(_conversation);
            return base.WaitResults();
        }
    }
}