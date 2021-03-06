﻿// <auto-generated />
using System;
using BridgeBotNext.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace BridgeBotNext.Migrations
{
    [DbContext(typeof(BotDbContext))]
    [Migration("20200221190137_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("BridgeBotNext.Entities.Connection", b =>
                {
                    b.Property<long>("ConnectionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("Direction")
                        .HasColumnType("integer");

                    b.Property<string>("LeftConversationConversationId")
                        .HasColumnType("text");

                    b.Property<string>("RightConversationConversationId")
                        .HasColumnType("text");

                    b.Property<string>("Token")
                        .HasColumnType("text");

                    b.HasKey("ConnectionId");

                    b.HasIndex("LeftConversationConversationId");

                    b.HasIndex("RightConversationConversationId");

                    b.ToTable("Connections");
                });

            modelBuilder.Entity("BridgeBotNext.Entities.Conversation", b =>
                {
                    b.Property<string>("ConversationId")
                        .HasColumnType("text");

                    b.Property<string>("Title")
                        .HasColumnType("text");

                    b.HasKey("ConversationId");

                    b.ToTable("Conversations");
                });

            modelBuilder.Entity("BridgeBotNext.Entities.Person", b =>
                {
                    b.Property<string>("PersonId")
                        .HasColumnType("text");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("DisplayName")
                        .HasColumnType("text");

                    b.Property<int>("IsAdminInt")
                        .HasColumnName("IsAdmin")
                        .HasColumnType("integer");

                    b.HasKey("PersonId");

                    b.ToTable("Persons");

                    b.HasDiscriminator<string>("Discriminator").HasValue("Person");
                });

            modelBuilder.Entity("BridgeBotNext.Providers.Tg.TgPerson", b =>
                {
                    b.HasBaseType("BridgeBotNext.Entities.Person");

                    b.Property<string>("Username")
                        .HasColumnType("text");

                    b.HasDiscriminator().HasValue("TgPerson");
                });

            modelBuilder.Entity("BridgeBotNext.Providers.Vk.VkPerson", b =>
                {
                    b.HasBaseType("BridgeBotNext.Entities.Person");

                    b.HasDiscriminator().HasValue("VkPerson");
                });

            modelBuilder.Entity("BridgeBotNext.Entities.Connection", b =>
                {
                    b.HasOne("BridgeBotNext.Entities.Conversation", "LeftConversation")
                        .WithMany()
                        .HasForeignKey("LeftConversationConversationId");

                    b.HasOne("BridgeBotNext.Entities.Conversation", "RightConversation")
                        .WithMany()
                        .HasForeignKey("RightConversationConversationId");
                });
#pragma warning restore 612, 618
        }
    }
}
