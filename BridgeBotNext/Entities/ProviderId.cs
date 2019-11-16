
using System;
using System.Collections.Generic;

using BridgeBotNext.Providers;

namespace BridgeBotNext.Entities
{
    public class ProviderId
    {
        public ProviderId()
        {
        }

        public ProviderId(Provider provider, string id)
        {
            Provider = provider;
            Id = id;
        }

        public Provider Provider { get; set; }
        public string Id { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ProviderId id &&
                   EqualityComparer<Provider>.Default.Equals(Provider, id.Provider) &&
                   Id == id.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Provider, Id);
        }

        public override string ToString()
        {
            return $"{Provider.Name}:{Id}";
        }
    }
}