namespace BridgeBotNext.Providers.Tg
{
    /// <inheritdoc />
    public class TgPerson : Person
    {
        public TgPerson(TgProvider provider, string id, string displayName)
        {
            Provider = provider;
            Id = id;
            DisplayName = displayName;
        }

        public override Provider Provider { get; }
        public override string Id { get; }
        public override string DisplayName { get; }

        //public override string ProfileUrl => $"https://t.me/{Id}";
        public override string ProfileUrl => $"https://t-do.ru/{Id}";
    }
}