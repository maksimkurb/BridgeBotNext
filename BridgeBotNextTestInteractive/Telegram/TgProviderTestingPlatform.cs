using System.Threading.Tasks;

namespace BridgeBotNextTest.Telegram
{
    public class TgProviderTestingPlatform : ProviderTestingPlatform
    {
        public TgProviderTestingPlatform(TestTgProvider provider) : base(provider)
        {
        }

        public override Task<bool> WaitResults(bool sendPassOrFail = true)
        {
            var provider = (TestTgProvider) _provider;
            if (sendPassOrFail) provider.SendTestButtons(_conversation);

            return base.WaitResults(false);
        }
    }
}