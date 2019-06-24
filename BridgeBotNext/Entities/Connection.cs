using System;
using BridgeBotNext.Entities;
using LiteDB;

namespace BridgeBotNext
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
        [BsonId] public ObjectId ConnectionId { get; set; }

        [BsonRef("conversations")] public Conversation LeftConversation { get; set; }

        [BsonRef("conversations")] public Conversation RightConversation { get; set; }

        public ConnectionDirection Direction { get; set; }

        public string Token { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}