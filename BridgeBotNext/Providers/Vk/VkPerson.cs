namespace BridgeBotNext.Providers.Vk
{
    /// <inheritdoc />
    public class VkPerson : Person
    {
        public VkPerson(Provider provider, string id, string displayName)
        {
            Provider = provider;
            Id = id;
            DisplayName = displayName;
        }

        public override Provider Provider { get; }
        public override string Id { get; }
        public override string DisplayName { get; }
        public override string ProfileUrl => $"https://vk.com/id{Id}";
    }
}