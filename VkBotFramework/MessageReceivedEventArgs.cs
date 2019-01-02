using System;
using VkNet.Model;

namespace VkBotFramework
{
    public partial class VkBot
    {
        public class MessageReceivedEventArgs : EventArgs
        {
            public Message message;

            public MessageReceivedEventArgs(Message message)
            {
                this.message = message;
            }
        }
    }
}