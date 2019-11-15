using System;
using System.Threading.Tasks;

using BridgeBotNext.Providers;

using LiteDB;

namespace BridgeBotNext.Entities
{

    public class Conversation
    {
        public Conversation() { }
        public Conversation(Provider provider, string id) : this()
        {
            ProviderId = new ProviderId(provider, id);
        }
        public Conversation(Provider provider, string id, string title) : this(provider, id)
        {
            Title = title;
        }


        /// <summary>
        /// Composite key: provider + conversationId
        /// </summary>
        [BsonId] public ProviderId ProviderId { get; set; }

        /// <summary>
        /// Original provider
        /// </summary>
        [BsonIgnore] public Provider Provider => ProviderId.Provider;

        /// <summary>
        /// Original conversation ID
        /// </summary>
        [BsonIgnore] public string OriginId => ProviderId.Id;

        public string Title { get; set; }

        public Task SendMessage(Message message)
        {
            return Provider.SendMessage(this, message);
        }


        public override string ToString()
        {
            return $"[{Provider.DisplayName}] {Title}";
        }

        protected bool Equals(Conversation other) => ProviderId.Equals(other.ProviderId);
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Conversation)obj);
        }


        public override int GetHashCode()
        {
            return HashCode.Combine(ProviderId);
        }
    }
}