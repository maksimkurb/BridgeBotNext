using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

using BridgeBotNext.Providers;

namespace BridgeBotNext.Entities
{

    public class Conversation
    {
        public Conversation() { }
        public Conversation(Provider provider, string id) : this()
        {
            ConversationId = new ProviderId(provider, id);
        }
        public Conversation(Provider provider, string id, string title) : this(provider, id)
        {
            Title = title;
        }


        /// <summary>
        /// Composite key: provider + conversationId
        /// </summary>
        public ProviderId ConversationId { get; set; }

        /// <summary>
        /// Original provider
        /// </summary>
        [NotMapped] public Provider Provider => ConversationId.Provider;

        /// <summary>
        /// Original conversation ID
        /// </summary>
        [NotMapped] public string OriginId => ConversationId.Id;

        public string Title { get; set; }

        public Task SendMessage(Message message)
        {
            return Provider.SendMessage(this, message);
        }


        public override string ToString()
        {
            return $"[{Provider.DisplayName}] {Title}";
        }

        protected bool Equals(Conversation other) => ConversationId.Equals(other.ConversationId);
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Conversation)obj);
        }


        public override int GetHashCode()
        {
            return HashCode.Combine(ConversationId);
        }
    }
}