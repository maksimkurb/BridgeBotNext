using BridgeBotNext.Providers;

namespace BridgeBotNext.Entities
{
    public abstract class Person
    {
        public abstract Provider Provider { get; }

        /// <summary>
        ///     Person unique id
        /// </summary>
        public abstract string PersonId { get; }

        /// <summary>
        ///     Person display name
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        ///     Person profile url
        /// </summary>
        public abstract string ProfileUrl { get; }

        protected bool Equals(Person other)
        {
            return Provider.Equals(other.Provider) && string.Equals(PersonId, other.PersonId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Person) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Provider != null ? Provider.GetHashCode() : 0) * 397) ^
                       (PersonId != null ? PersonId.GetHashCode() : 0);
            }
        }
    }
}