using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BridgeBotNext.Entities
{
    public enum ConnectionDirection
    {
        TwoWay = 0,
        ToRight,
        ToLeft,
        None
    }

    public class Connection
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ConnectionId { get; set; }

        public Conversation LeftConversation { get; set; }

        public Conversation RightConversation { get; set; }

        public ConnectionDirection Direction { get; set; }

        public string Token { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}