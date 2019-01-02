using System;
using VkNet.Model.GroupUpdate;

namespace VkBotFramework
{
    public partial class VkBot
    {
        public class GroupUpdateReceivedEventArgs : EventArgs
        {
            public GroupUpdate update;

            public GroupUpdateReceivedEventArgs(GroupUpdate update)
            {
                this.update = update;
            }
        }
    }
}