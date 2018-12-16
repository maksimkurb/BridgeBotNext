using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            int prevLevel = 0;
            foreach (var (msg, level) in messages)
            {
                if (msg.OriginSender != null)
                {
                    if (prevSender == null || prevLevel != level || !prevSender.Equals(msg.OriginSender)) {
                        sb.Append("|");
                        for (int i = 0; i <= level; i++)
                        {
                            sb.Append("›");
                        }
    
                        sb.Append(FormatSender(msg.OriginSender));
                        sb.Append("\n");
                    }
                }
                var rows = msg.Body.Split("\n");
                foreach (var row in rows)
                {
                    sb.Append("|");
                    for (int i = 0; i <= level; i++)
                    {
                        sb.Append("›");
                    }
                    sb.Append(row);
                    sb.Append("\n");
                }

                prevSender = msg.OriginSender;
                prevLevel = level;
            }

            return sb.ToString();
        }

        public virtual string FormatSender(Person sender)
        {
            return $"💬 {sender.DisplayName}:";
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