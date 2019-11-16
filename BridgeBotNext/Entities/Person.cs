using System;
using System.ComponentModel.DataAnnotations.Schema;
using BridgeBotNext.Providers;

namespace BridgeBotNext.Entities
{
    public abstract class Person
    {
        public ProviderId PersonId { get; set; }

        protected Person()
        {
        }

        protected Person(Provider provider, string id) : this()
        {
            this.PersonId = new ProviderId(provider, id);
        }
        protected Person(Provider provider, string id, string displayName) : this(provider, id)
        {
            this.DisplayName = displayName;
        }

        [NotMapped] public Provider Provider => PersonId.Provider;

        /// <summary>
        ///     Person unique id
        /// </summary>
        [NotMapped] public string OriginId => PersonId.Id;

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
        [NotMapped] public bool IsAdmin
        {
            get => this.IsAdminInt != 0;
            set => this.IsAdminInt = (value ? 1 : 0);
        }

        [Column("IsAdmin")] public int IsAdminInt { get; set; } = 0;

        protected bool Equals(Person other) => PersonId.Equals(other.PersonId);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Person)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PersonId);
        }
    }
}