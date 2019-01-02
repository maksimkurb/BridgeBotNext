using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BridgeBotNext.Attachments;

namespace BridgeBotNext.Providers
{
    public abstract class Provider : IDisposable
    {
        /**
         * Provider unique name
         */
        public virtual string Name => "";

        /**
         * Provider display name
         */
        public virtual string DisplayName => "";

        public virtual void Dispose()
        {
            Disconnect();
        }

        /**
         * Start provider and connect to the messaging service 
         */
        public abstract Task Connect();

        /*
         * Disconnect from the messaging service and stop provider
         */
        public abstract Task Disconnect();

        /**
         * Send message to provider
         */
        public abstract Task SendMessage(Conversation conversation, Message message);

        public virtual string FormatForwardedMessages(IEnumerable<(Message Item, int Level)> messages)
        {
            StringBuilder sb = new StringBuilder();

            Person prevSender = null;
            int prevLevel = Int32.MaxValue;
            foreach (var (msg, level) in messages)
            {
                if (msg.OriginSender != null)
                {
                    if (prevSender == null || prevLevel != level || !prevSender.Equals(msg.OriginSender))
                    {
                        sb.Append("| ");
                        for (int i = 0; i <= level; i++)
                        {
                            sb.Append(">⁣"); // invisible separator because VK translates ">>" into "»"
                        }

                        sb.Append("| ");
                        sb.Append(FormatSender(msg.OriginSender));
                        sb.Append("\n");
                    }
                }

                var rows = msg.Body.Split("\n");
                foreach (var row in rows)
                {
                    sb.Append("| ");
                    for (int i = 0; i <= level; i++)
                    {
                        sb.Append(">⁣"); // same as above
                    }

                    sb.Append("| ");
                    sb.Append(row);
                    sb.Append("\n");
                }

                prevSender = msg.OriginSender;
                prevLevel = level;
            }

            return sb.ToString();
        }

        protected virtual string FormatSender(Person sender)
        {
            return $"💬 {sender.DisplayName}:";
        }

        protected virtual string FormatMessageBody(Message message,
            (Message Item, int Level)[] forwardedMessages = null)
        {
            var body = new StringBuilder();

            if (message.OriginSender != null)
            {
                body.AppendLine(FormatSender(message.OriginSender));
            }

            if (!forwardedMessages.IsNullOrEmpty())
            {
                body.AppendLine(FormatForwardedMessages(forwardedMessages));
            }

            if (!string.IsNullOrEmpty(message.Body))
            {
                body.AppendLine(message.Body);
            }

            return body.ToString();
        }

        protected virtual (Message Item, int Level)[] FlattenForwardedMessages(Message message)
        {
            var fwdExpanded = message.ForwardedMessages?
                .PostOrderFlatten(el => el.ForwardedMessages);
            return fwdExpanded as (Message Item, int Level)[] ?? fwdExpanded?.ToArray();
        }

        protected virtual List<Attachment> GetAllAttachments(Message message, (Message Item, int Level)[] fwd)
        {
            List<Attachment> attachments = new List<Attachment>();
            if (!fwd.IsNullOrEmpty())
            {
                attachments.AddRange(fwd
                    .Select(e => e.Item)
                    .Where(msg => !msg.Attachments.IsNullOrEmpty())
                    .SelectMany(msg => msg.Attachments)
                );
            }

            if (message != null && !message.Attachments.IsNullOrEmpty())
            {
                attachments.AddRange(message.Attachments);
            }

            return attachments;
        }

        /**
         * Message received event
         */
        public event EventHandler<MessageEventArgs> MessageReceived;

        protected virtual void OnMessageReceived(MessageEventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            var handler = MessageReceived;
            handler?.Invoke(this, e);
        }

        /**
         * Command received event
         */
        public event EventHandler<MessageEventArgs> CommandReceived;

        protected virtual void OnCommandReceived(MessageEventArgs e)
        {
            var handler = CommandReceived;
            handler?.Invoke(this, e);
        }


        public class MessageEventArgs : EventArgs
        {
            public MessageEventArgs(Message message)
            {
                Message = message;
            }

            public Message Message { get; }
        }
    }
}