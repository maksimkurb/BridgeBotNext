using System;
using BridgeBotNext.Entities;

namespace BridgeBotNext.Providers.Vk
{
    /// <inheritdoc />
    public class VkPerson : Person
    {
        public VkPerson(Provider provider, string id, string displayName)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Person id can not be empty", nameof(id));

            Provider = provider;
            PersonId = id;
            DisplayName = displayName;
        }

        public override Provider Provider { get; }
        public override string PersonId { get; }
        public override string DisplayName { get; }
        public override string ProfileUrl => PersonId.StartsWith("-") ? $"https://vk.com/club{PersonId.Substring(1)}" : $"https://vk.com/id{PersonId}";
    }
}