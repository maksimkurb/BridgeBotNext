using System.Threading.Tasks;
using BridgeBotNext.Providers;
using LiteDB;

namespace BridgeBotNext.Entities
{
    public class ConversationId
    {
        public ConversationId()
        {
        }

        public ConversationId(Provider provider, string id)
        {
            Provider = provider;
            Id = id;
        }

        public Provider Provider { get; set; }
        public string Id { get; set; }

        public override string ToString()
        {
            return $"{Provider.Name}:{Id}";
        }
    }

    public class Conversation
    {
        public Conversation()
        {
        }

        public Conversation(Provider provider, string id, string title)
        {
            ConversationId = new ConversationId(provider, id);
            Title = title;
        }

        public Conversation(Provider provider, string id)
        {
            ConversationId = new ConversationId(provider, id);
        }

        [BsonId] public ConversationId ConversationId { get; set; }

        [BsonIgnore] public Provider Provider => ConversationId.Provider;

        [BsonIgnore] public string Id => ConversationId.Id;

        public string Title { get; set; }

        public Task SendMessage(Message message)
        {
            return Provider.SendMessage(this, message);
        }

        protected bool Equals(Conversation other)
        {
            return Equals(Provider, other.Provider) && string.Equals(Id, other.Id);
        }

        public override string ToString()
        {
            return $"[{Provider.DisplayName}] {Title}";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Conversation) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Provider != null ? Provider.GetHashCode() : 0) * 397) ^ (Id != null ? Id.GetHashCode() : 0);
            }
        }
    }
}