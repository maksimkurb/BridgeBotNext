using BridgeBotNext.Providers;

namespace BridgeBotNext
{
    public class Conversation
    {
        public Provider Provider { get; private set; }
        public string Id { get; private set; }
        public string Title { get; set; }

        public Conversation(Provider provider, string id, string title)
        {
            Provider = provider;
            Id = id;
            Title = title;
        }

        public Conversation(Provider provider, string id)
        {
            Provider = provider;
            Id = id;
        }
    }
}