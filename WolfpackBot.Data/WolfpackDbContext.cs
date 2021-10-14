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
                    opt.Navigation(opt => opt.EventSignups).AutoInclude();
                });

            modelBuilder.Entity<Leaderboard>(opt =>
            {
                opt.HasMany(lb => lb.LeaderboardEntries).WithOne(lbe => lbe.Leaderboard);
                opt.Navigation(lb => lb.LeaderboardEntries).AutoInclude();
            });
            modelBuilder.Entity<LeaderboardEntry>(opt => opt.ToTable("LeaderboardEntries"));
        }
    }
}
