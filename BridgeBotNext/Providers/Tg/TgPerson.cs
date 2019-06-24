using BridgeBotNext.Entities;

namespace BridgeBotNext.Providers.Tg
{
    /// <inheritdoc />
    public class TgPerson : Person
    {
        public TgPerson(TgProvider provider, int id, string displayName)
        {
            Provider = provider;
            NumericPersonId = id;
            DisplayName = displayName;
        }

        private int NumericPersonId { get; }
        public override Provider Provider { get; }
        public override string PersonId => NumericPersonId.ToString();
        public override string DisplayName { get; }

        public override string ProfileUrl => $"https://t.me/{PersonId}";
    }
}