using BridgeBotNext.Attachments;

namespace BridgeBotNext
{
    public class Message
    {
        public Conversation OriginConversation { get; }
        public Person OriginSender { get; }

        public string Body { get; }
        public Message[] ForwardedMessages { get; }
        public Attachment[] Attachments { get; }

        public Message(Conversation originConversation, Person originSender, string body, Message[] forwardedMessages, Attachment[] attachments)
        {
            OriginConversation = originConversation;
            OriginSender = originSender;
            Body = body;
            ForwardedMessages = forwardedMessages;
            Attachments = attachments;
        }

        public Message(Conversation originConversation, Person originSender, string body)
        {
            OriginConversation = originConversation;
            OriginSender = originSender;
            Body = body;
        }
    }
}