using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBotNext.Providers;
using BridgeBotNext.Providers.Tg;
using BridgeBotNext.Providers.Vk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BridgeBotNext.Entities
{
    public class BotDbContext: DbContext
    {
        public DbSet<Connection> Connections { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Person> Persons { get; set; }

        private readonly IEnumerable<Provider> _providers;

        public BotDbContext(DbContextOptions<BotDbContext> options, IEnumerable<Provider> providers)
            : base(options)
        {
            _providers = providers;
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .UseIdentityColumns();
            
            var providerConverter = new ValueConverter<Provider, string>(
                v => v.ToString(),
                value => _providers.First(p => p.Name.Equals(value)));
            
            var providerIdConverter = new ValueConverter<ProviderId, string>(
                v => v.ToString(),
                value => _providerIdFromString(value));

            modelBuilder
                .Entity<Conversation>()
                .Property(c => c.ConversationId)
                .HasConversion(providerIdConverter);
            modelBuilder
                .Entity<Person>()
                .Property(p => p.PersonId)
                .HasConversion(providerIdConverter);
            
            modelBuilder
                .Entity<VkPerson>()
                .HasBaseType<Person>();
            
            modelBuilder
                .Entity<TgPerson>()
                .HasBaseType<Person>();
            
        }

        protected ProviderId _providerIdFromString(string value)
        {
            var parts = value.Split(':', 2);
            if (parts.Length != 2) return null;
            foreach (var provider in _providers)
                if (provider.Name == parts[0])
                    return new ProviderId(provider, parts[1]);

            return null;
        }
    }
}