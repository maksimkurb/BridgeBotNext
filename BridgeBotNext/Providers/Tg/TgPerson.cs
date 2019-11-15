
using BridgeBotNext.Entities;

namespace BridgeBotNext.Providers.Tg
{
    /// <inheritdoc />
    public class TgPerson : Person
    {
        public TgPerson() : base() { }

        public TgPerson(TgProvider provider, int id, string displayName) : base(provider, id.ToString(), displayName)
        {
        }
        public TgPerson(TgProvider provider, int id, string username, string displayName) : this(provider, id, displayName)
        {
            this.Username = username;
        }

        public string Username { get; set; }

        public override string ProfileUrl => !string.IsNullOrEmpty(Username) ? $"https://t.me/{Username}" : "<no_username>";

    }
}