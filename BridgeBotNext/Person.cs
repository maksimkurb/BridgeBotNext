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
    }
}