using System;

using BridgeBotNext.Providers;

using LiteDB;

namespace BridgeBotNext.Entities
{
    public abstract class Person
    {
        [BsonId]
        public ProviderId ProviderId { get; set; }

        protected Person()
        {
        }

        protected Person(Provider provider, string id) : this()
        {
            this.ProviderId = new ProviderId(provider, id);
        }
        protected Person(Provider provider, string id, string displayName) : this(provider, id)
        {
            this.DisplayName = displayName;
        }

        public Provider Provider => ProviderId.Provider;

        /// <summary>
        ///     Person unique id
        /// </summary>
        public string OriginId => ProviderId.Id;

        /// <summary>
        ///     Person display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        ///     Person profile url
        /// </summary>
        public abstract string ProfileUrl { get; }

        /// <summary>
        ///     Does person has rights to use bot commands
        /// </summary>
        public bool IsAdmin { get; set; } = false;

        protected bool Equals(Person other) => ProviderId.Equals(other.ProviderId);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Person)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProviderId);
        }
    }
}