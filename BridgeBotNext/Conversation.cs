using BridgeBotNext.Providers;

namespace BridgeBotNext
{
    public class Conversation
    {
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

        public Provider Provider { get; }
        public string Id { get; }
        public string Title { get; set; }
    }
}