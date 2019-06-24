using System;

using BridgeBotNext.Entities;

namespace BridgeBotNext.Providers.Vk
{
    /// <inheritdoc />
    public class VkPerson : Person
    {
        public VkPerson() : base() { }

        public VkPerson(Provider provider, string id, string displayName) : base(provider, id, displayName)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Person id can not be empty", nameof(id));
        }

        public override string ProfileUrl => OriginId.StartsWith("-") ? $"https://vk.com/club{OriginId.Substring(1)}" : $"https://vk.com/id{OriginId}";
    }
}