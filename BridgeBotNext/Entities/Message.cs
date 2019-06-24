using System.Collections.Generic;
using BridgeBotNext.Attachments;

namespace BridgeBotNext.Entities
{
    public class Message
    {
        public Message(Conversation originConversation = null, Person originSender = null, string body = null,
            IEnumerable<Message> forwardedMessages = null, IEnumerable<Attachment> attachments = null)
        {
            OriginConversation = originConversation;
            OriginSender = originSender;
            Body = body;
            ForwardedMessages = forwardedMessages;
            Attachments = attachments;
        }

        public Message(string body) : this(null, null, body)
        {
        }

        public Conversation OriginConversation { get; }
        public Person OriginSender { get; }

        public string Body { get; }
        public IEnumerable<Message> ForwardedMessages { get; }
        public IEnumerable<Attachment> Attachments { get; }

        public static implicit operator Message(string body)
        {
            return new Message(body);
        }
    }
}