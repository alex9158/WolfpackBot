using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WolfpackBot.Data.Models;

namespace WolfpackBot.Data
{
    public class WolfpackDbContext : DbContext
    {

        public DbSet<Event> Events { get; set; }

        public DbSet<Leaderboard> Leaderboards { get; set; }
        public DbSet<LeaderboardEntry> LeaderboardEntries { get; set; }
        public DbSet<LeaderboardModerator> LeaderboardModerators { get; set; }
        public DbSet<ChampionshipResultsModel> ChampionshipResults { get; set; }
        public DbSet<EventAliasMappingModel> EventAliasMapping { get; set; }
        public DbSet<ExcelSheetEventMappingModel> ExcelSheetEventMapping { get; set; }

        public WolfpackDbContext(DbContextOptions<WolfpackDbContext> options) : base(options)
        {

        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<Event>(opt =>
                {
                    opt.ToTable("Event")
                       .HasMany(e => e.EventSignups)
                       .WithOne(e => e.@event);
                    opt.Property(e => e.StandingsMessageIds)
                        .HasConversion(
                            v => string.Join(',', v),
                            v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));
                    opt.Navigation(opt => opt.EventSignups).AutoInclude();
                });

            modelBuilder.Entity<Leaderboard>(opt =>
            {
                opt.HasMany(lb => lb.LeaderboardEntries).WithOne(lbe => lbe.Leaderboard);
                opt.Navigation(lb => lb.LeaderboardEntries).AutoInclude();
            });
            modelBuilder.Entity<LeaderboardEntry>(opt => opt.ToTable("LeaderboardEntries"));
            modelBuilder.Entity<LeaderboardModerator>(opt => opt.HasKey(mod => new { mod.GuildId, mod.RoleId }));
            modelBuilder.Entity<ChampionshipResultsModel>(opt => opt
                .ToTable("ChampionshipResults")
                .HasOne(res => res.Event)
                .WithMany(@event => @event.ChampionshipResults));
            modelBuilder.Entity<EventAliasMappingModel>(opt => opt
                .ToTable("EventAliasMapping")
                .HasOne(mapping => mapping.Event)
                .WithMany(e => e.EventAliasMappings));
            modelBuilder.Entity<ExcelSheetEventMappingModel>(opt => opt
                .ToTable("ExcelSheetEventMapping")
                .HasOne(mapping => mapping.Event)
                .WithMany(e => e.ExcelSheetEventMappings));
        }
    }
}
