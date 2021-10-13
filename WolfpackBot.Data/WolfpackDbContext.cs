using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using WolfpackBot.Models;

namespace WolfpackBot.Data
{
    public class WolfpackDbContext: DbContext
    {

        public DbSet<Event> Events { get; set; }
        public DbSet<EventsWithCount> EventsWithCount { get; set; }
        public WolfpackDbContext(DbContextOptions<WolfpackDbContext> options):base(options)
        {

        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<EventsWithCount>(opt => opt.ToView("EventsWithCount"))
                .Entity<Event>(opt => opt
                    .HasMany(e => e.EventSignups)
                    .WithOne(e => e.@event));
        }
    }
}
