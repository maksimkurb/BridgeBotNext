using BridgeBotNext.Providers;

namespace BridgeBotNext
{
    public abstract class Person
    {
        public abstract Provider Provider { get; }

        /**
         * Person unique id
         */
        public abstract string Id { get; }

        /**
         * Person display name
         */
        public abstract string DisplayName { get; }

        /**
         * Person profile url
         */
        public abstract string ProfileUrl { get; }

        protected bool Equals(Person other)
        {
            return Equals(Provider, other.Provider) && string.Equals(Id, other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Person) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Provider != null ? Provider.GetHashCode() : 0) * 397) ^ (Id != null ? Id.GetHashCode() : 0);
            }
        }
    }
}